using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Domain;

namespace Binah.Ontology.Models.Factory;

/// <summary>
    /// Factory class for creating entity instances based on type
    /// </summary>
    public static class EntityFactory
    {
        /// <summary>
        /// Creates an entity instance based on type string
        /// </summary>
        public static Entity CreateEntity(string type, string? id = null)
        {
            var entity = type switch
            {
                "Property" => new Property(),
                "Entity" => new LegalEntity(),
                "Person" => new Person(),
                "Zone" => new Zone(),
                "Document" => new Document(),
                _ => new Entity { Type = type }
            };

            entity.Id = id ?? Guid.NewGuid().ToString();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            return entity;
        }

        /// <summary>
        /// Creates an entity with initial properties
        /// </summary>
        public static Entity CreateEntity(
            string type, 
            string? id = null, 
            Dictionary<string, object>? properties = null,
            string? tenantId = null,
            string? createdBy = null)
        {
            var entity = CreateEntity(type, id);

            if (properties != null)
            {
                entity.Properties = properties;
            }

            entity.TenantId = tenantId;
            entity.CreatedBy = createdBy;
            entity.UpdatedBy = createdBy;

            return entity;
        }
    }