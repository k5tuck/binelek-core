using Binah.Ontology.Models.Exceptions;

namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// RELATED_TO relationship: Generic catch-all relationship between entities
    /// </summary>
    public class RelatedToRelationship : Relationship
    {
        public RelatedToRelationship()
        {
            Type = "RELATED_TO";
        }

        /// <summary>Specific relationship type descriptor</summary>
        public string? RelationshipType
        {
            get => GetPropertyValue<string>("relationship_type");
            set => SetPropertyValue("relationship_type", value);
        }

        /// <summary>Confidence of extracted relationship (0.0 - 1.0)</summary>
        public double? ExtractedConfidence
        {
            get => GetPropertyValue<double?>("extracted_confidence");
            set
            {
                if (value.HasValue && (value < 0 || value > 1))
                    throw new RelationshipCreationException("RELATED_TO", "Extracted confidence must be between 0 and 1");
                SetPropertyValue("extracted_confidence", value);
            }
        }
    }