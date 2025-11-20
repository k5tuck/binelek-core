
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Domain;
using Binah.Ontology.Models.Exceptions;
namespace Binah.Ontology.Models.Extensions;

/// <summary>
    /// Extension methods for entity validation and lifecycle management
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Validates that all required base fields are present
        /// </summary>
        public static void ValidateRequired(this Entity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
                throw new EntityCreationException(entity.Type, "Id is required and cannot be empty");

            if (string.IsNullOrWhiteSpace(entity.Type))
                throw new EntityCreationException("Entity", "Type is required and cannot be empty");
        }

        /// <summary>
        /// Validates property-specific required fields
        /// </summary>
        public static void ValidateRequired(this Property property)
        {
            ((Entity)property).ValidateRequired();

            if (string.IsNullOrWhiteSpace(property.Uid))
                throw new EntityCreationException("Property", "Uid is required and cannot be empty");
        }

        /// <summary>
        /// Validates legal entity-specific required fields
        /// </summary>
        public static void ValidateRequired(this LegalEntity entity)
        {
            ((Entity)entity).ValidateRequired();

            if (string.IsNullOrWhiteSpace(entity.Name))
                throw new EntityCreationException("Entity", "Name is required and cannot be empty");
        }

        /// <summary>
        /// Validates zone-specific required fields
        /// </summary>
        public static void ValidateRequired(this Zone zone)
        {
            ((Entity)zone).ValidateRequired();

            if (string.IsNullOrWhiteSpace(zone.Name))
                throw new EntityCreationException("Zone", "Name is required and cannot be empty");
        }

        /// <summary>
        /// Validates person-specific required fields
        /// </summary>
        public static void ValidateRequired(this Person person)
        {
            ((Entity)person).ValidateRequired();

            if (string.IsNullOrWhiteSpace(person.FullName) && 
                string.IsNullOrWhiteSpace(person.FirstName) && 
                string.IsNullOrWhiteSpace(person.LastName))
            {
                throw new EntityCreationException("Person", "At least one name field (FirstName, LastName, or FullName) is required");
            }
        }

        /// <summary>
        /// Validates document-specific required fields
        /// </summary>
        public static void ValidateRequired(this Document document)
        {
            ((Entity)document).ValidateRequired();

            if (string.IsNullOrWhiteSpace(document.DocumentId) && string.IsNullOrWhiteSpace(document.FileUrl))
                throw new EntityCreationException("Document", "Either DocumentId or FileUrl must be provided");
        }

        /// <summary>
        /// Marks entity as deleted (soft delete)
        /// </summary>
        public static void MarkDeleted(this Entity entity, string? deletedBy = null)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = deletedBy;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = deletedBy;
        }

        /// <summary>
        /// Restores a soft-deleted entity
        /// </summary>
        public static void Restore(this Entity entity, string? restoredBy = null)
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedBy = null;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = restoredBy;
        }

        /// <summary>
        /// Updates the version number for lineage tracking
        /// </summary>
        public static void IncrementVersion(this Entity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Version) || entity.Version == "1.0")
            {
                entity.Version = "1.1";
                return;
            }

            var parts = entity.Version.Split('.');
            if (parts.Length == 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            {
                entity.Version = $"{major}.{minor + 1}";
            }
        }

        /// <summary>
        /// Adds metadata to an entity
        /// </summary>
        public static void AddMetadata(this Entity entity, string key, object value)
        {
            entity.Metadata ??= new Dictionary<string, object>();
            entity.Metadata[key] = value;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
