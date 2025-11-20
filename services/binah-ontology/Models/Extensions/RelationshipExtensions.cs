using Binah.Ontology.Models.SupportModels;
using Binah.Core.Exceptions;
using Binah.Ontology.Models.Relationship;
using Binah.Ontology.Models.Exceptions;

namespace Binah.Ontology.Models.Extensions;

// <summary>
    /// Extension methods for relationship validation and lifecycle management
    /// </summary>
    public static class RelationshipExtensions
    {
        /// <summary>
        /// Validates that all required relationship fields are present
        /// </summary>
        public static void ValidateRequired(this Relationship.Relationship relationship)
        {
            if (string.IsNullOrWhiteSpace(relationship.Type))
                throw new RelationshipCreationException("Unknown", "Type is required and cannot be empty");

            if (string.IsNullOrWhiteSpace(relationship.FromEntityId))
                throw new RelationshipCreationException(relationship.Type, "FromEntityId is required and cannot be empty");

            if (string.IsNullOrWhiteSpace(relationship.ToEntityId))
                throw new RelationshipCreationException(relationship.Type, "ToEntityId is required and cannot be empty");

            if (relationship.ConfidenceScore < 0 || relationship.ConfidenceScore > 1)
                throw new RelationshipCreationException(relationship.Type, "ConfidenceScore must be between 0 and 1");
        }

        /// <summary>
        /// Adds a source record to the relationship
        /// </summary>
        public static void AddSourceRecord(this Relationship.Relationship relationship, string source, string sourceId)
        {
            relationship.SourceRecords.Add(new SourceRecord
            {
                Source = source,
                SourceId = sourceId,
                IngestTime = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Adds multiple source records to the relationship
        /// </summary>
        public static void AddSourceRecords(this Relationship.Relationship relationship, IEnumerable<SourceRecord> records)
        {
            foreach (var record in records)
            {
                relationship.SourceRecords.Add(record);
            }
        }

        /// <summary>
        /// Sets the confidence score with validation
        /// </summary>
        public static void SetConfidenceScore(this Relationship.Relationship relationship, double score)
        {
            if (score < 0 || score > 1)
                throw new RelationshipCreationException(relationship.Type, "Confidence score must be between 0 and 1");
            
            relationship.ConfidenceScore = score;
        }
    }