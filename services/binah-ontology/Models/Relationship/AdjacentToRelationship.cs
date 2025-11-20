namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// ADJACENT_TO relationship: Property is adjacent to another Property
    /// Direction: Property -> Property
    /// </summary>
    public class AdjacentToRelationship : Relationship
    {
        public AdjacentToRelationship()
        {
            Type = "ADJACENT_TO";
        }

        /// <summary>Length of shared boundary (meters)</summary>
        public double? BoundaryLength
        {
            get => GetPropertyValue<double?>("boundary_length");
            set => SetPropertyValue("boundary_length", value);
        }
    }