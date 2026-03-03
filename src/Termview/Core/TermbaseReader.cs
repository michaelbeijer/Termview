using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Termview.Models;

namespace Termview.Core
{
    /// <summary>
    /// Reads termbases from Supervertaler's SQLite database (supervertaler.db).
    /// This allows sharing the same termbases between Supervertaler and Termview.
    /// </summary>
    public class TermbaseReader : IDisposable
    {
        private SQLiteConnection _connection;
        private readonly string _dbPath;
        private bool _disposed;

        public TermbaseReader(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        public bool Open()
        {
            if (!File.Exists(_dbPath))
                return false;

            try
            {
                // Do not use "Read Only=True" — it prevents SQLite from accessing
                // the SHM coordination file required when the DB is in WAL mode
                // (e.g. while Supervertaler has it open). We only run SELECTs anyway.
                var connStr = $"Data Source={_dbPath};Version=3;";
                _connection = new SQLiteConnection(connStr);
                _connection.Open();
                return true;
            }
            catch
            {
                _connection?.Dispose();
                _connection = null;
                return false;
            }
        }

        /// <summary>
        /// Gets all available termbases in the database.
        /// </summary>
        public List<TermbaseInfo> GetTermbases()
        {
            var result = new List<TermbaseInfo>();
            if (_connection == null) return result;

            const string sql = @"
                SELECT tb.id, tb.name, tb.source_lang, tb.target_lang,
                       tb.is_project_termbase, tb.ranking,
                       COUNT(t.id) as term_count
                FROM termbases tb
                LEFT JOIN termbase_terms t ON CAST(t.termbase_id AS INTEGER) = tb.id
                GROUP BY tb.id
                ORDER BY tb.ranking ASC, tb.name ASC";

            using (var cmd = new SQLiteCommand(sql, _connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new TermbaseInfo
                    {
                        Id = reader.GetInt64(0),
                        Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        SourceLang = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        TargetLang = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        IsProjectTermbase = !reader.IsDBNull(4) && reader.GetBoolean(4),
                        Ranking = reader.IsDBNull(5) ? 99 : reader.GetInt32(5),
                        TermCount = reader.GetInt32(6)
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Searches for terms matching the given word/phrase across all active termbases.
        /// Mirrors Supervertaler's search_termbases() logic.
        /// </summary>
        public List<TermEntry> SearchTerm(string searchTerm)
        {
            var results = new List<TermEntry>();
            if (_connection == null || string.IsNullOrWhiteSpace(searchTerm))
                return results;

            var normalised = searchTerm.Trim();

            const string sql = @"
                SELECT t.id, t.source_term, t.target_term, t.termbase_id,
                       t.source_lang, t.target_lang, t.definition, t.domain,
                       t.notes, t.forbidden, t.case_sensitive,
                       tb.name AS termbase_name,
                       tb.is_project_termbase,
                       COALESCE(tb.ranking, 99) AS ranking
                FROM termbase_terms t
                LEFT JOIN termbases tb ON CAST(t.termbase_id AS INTEGER) = tb.id
                WHERE (LOWER(t.source_term) = LOWER(@term)
                    OR LOWER(RTRIM(t.source_term, '.!?,;:')) = LOWER(@term)
                    OR LOWER(@term) = LOWER(RTRIM(t.source_term, '.!?,;:')))
                  AND COALESCE(t.forbidden, 0) = 0
                ORDER BY ranking ASC, t.source_term ASC";

            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@term", normalised);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entry = ReadTermEntry(reader);
                        results.Add(entry);
                    }
                }
            }

            // Load synonyms for each result
            foreach (var entry in results)
            {
                entry.TargetSynonyms = GetTargetSynonyms(entry.Id);
            }

            return results;
        }

        /// <summary>
        /// Bulk-loads all source terms for fast in-memory matching.
        /// Returns a dictionary mapping lowercased source term to list of entries.
        /// </summary>
        public Dictionary<string, List<TermEntry>> LoadAllTerms()
        {
            var index = new Dictionary<string, List<TermEntry>>(StringComparer.OrdinalIgnoreCase);
            if (_connection == null) return index;

            const string sql = @"
                SELECT t.id, t.source_term, t.target_term, t.termbase_id,
                       t.source_lang, t.target_lang, t.definition, t.domain,
                       t.notes, t.forbidden, t.case_sensitive,
                       tb.name AS termbase_name,
                       tb.is_project_termbase,
                       COALESCE(tb.ranking, 99) AS ranking
                FROM termbase_terms t
                LEFT JOIN termbases tb ON CAST(t.termbase_id AS INTEGER) = tb.id
                WHERE COALESCE(t.forbidden, 0) = 0
                ORDER BY ranking ASC";

            using (var cmd = new SQLiteCommand(sql, _connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var entry = ReadTermEntry(reader);
                    var key = entry.SourceTerm.Trim().ToLowerInvariant();

                    // Also index with trailing punctuation stripped
                    var stripped = key.TrimEnd('.', '!', '?', ',', ';', ':');

                    if (!index.ContainsKey(key))
                        index[key] = new List<TermEntry>();
                    index[key].Add(entry);

                    if (stripped != key && stripped.Length > 0)
                    {
                        if (!index.ContainsKey(stripped))
                            index[stripped] = new List<TermEntry>();
                        index[stripped].Add(entry);
                    }
                }
            }

            return index;
        }

        private List<string> GetTargetSynonyms(long termId)
        {
            var synonyms = new List<string>();
            if (_connection == null) return synonyms;

            const string sql = @"
                SELECT synonym_text FROM termbase_synonyms
                WHERE term_id = @termId AND language = 'target' AND forbidden = 0
                ORDER BY display_order ASC";

            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@termId", termId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            synonyms.Add(reader.GetString(0));
                    }
                }
            }

            return synonyms;
        }

        private static TermEntry ReadTermEntry(SQLiteDataReader reader)
        {
            return new TermEntry
            {
                Id = reader.GetInt64(0),
                SourceTerm = reader.IsDBNull(1) ? "" : reader.GetString(1),
                TargetTerm = reader.IsDBNull(2) ? "" : reader.GetString(2),
                TermbaseId = reader.IsDBNull(3) ? 0 : Convert.ToInt64(reader.GetValue(3)),
                SourceLang = reader.IsDBNull(4) ? "" : reader.GetString(4),
                TargetLang = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Definition = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Domain = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Notes = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Forbidden = !reader.IsDBNull(9) && Convert.ToBoolean(reader.GetValue(9)),
                CaseSensitive = !reader.IsDBNull(10) && Convert.ToBoolean(reader.GetValue(10)),
                TermbaseName = reader.IsDBNull(11) ? "" : reader.GetString(11),
                IsProjectTermbase = !reader.IsDBNull(12) && Convert.ToBoolean(reader.GetValue(12)),
                Ranking = reader.IsDBNull(13) ? 99 : reader.GetInt32(13)
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Close();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
