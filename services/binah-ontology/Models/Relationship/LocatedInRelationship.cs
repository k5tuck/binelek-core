namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// LOCATED_IN relationship: Property is located in Zone
    /// Direction: Property -> Zone
    /// </summary>
    public class LocatedInRelationship : Relationship
    {
        public LocatedInRelationship()
        {
            Type = "LOCATED_IN";
        }

        /// <summary>Distance to zone (meters) for nearest relationships</summary>
        public double? Distance
        {
            get => GetPropertyValue<double?>("distance");
            set => SetPropertyValue("distance", value);
        }

        /// <summary>Whether zone contains the property</summary>
        public bool? Contains
        {
            get => GetPropertyValue<bool?>("contains");
            set => SetPropertyValue("contains", value);
        }
    }