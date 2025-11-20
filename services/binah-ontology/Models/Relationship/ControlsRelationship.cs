using Binah.Ontology.Models.Exceptions;

namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// CONTROLS relationship: Entity controls another Entity
    /// Direction: Entity -> Entity
    /// </summary>
    public class ControlsRelationship : Relationship
    {
        public ControlsRelationship()
        {
            Type = "CONTROLS";
        }

        /// <summary>Control percentage (0.0 - 1.0)</summary>
        public double? ControlPct
        {
            get => GetPropertyValue<double?>("control_pct");
            set
            {
                if (value.HasValue && (value < 0 || value > 1))
                    throw new RelationshipCreationException("CONTROLS", "Control percentage must be between 0 and 1");
                SetPropertyValue("control_pct", value);
            }
        }

        /// <summary>Type of control: parent-subsidiary, common_owner</summary>
        public string? ControlType
        {
            get => GetPropertyValue<string>("control_type");
            set => SetPropertyValue("control_type", value);
        }
    }