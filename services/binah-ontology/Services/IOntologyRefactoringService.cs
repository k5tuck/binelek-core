using Binah.Ontology.Models.DTOs;

namespace Binah.Ontology.Services;

/// <summary>
/// Service for ontology refactoring PR creation
/// </summary>
public interface IOntologyRefactoringService
{
    /// <summary>
    /// Trigger ontology refactoring PR creation
    /// </summary>
    Task<OntologyRefactoringResponse> TriggerRefactoringPRAsync(OntologyChangeRequest request);

    /// <summary>
    /// Get refactoring status
    /// </summary>
    Task<OntologyRefactoringResponse> GetRefactoringStatusAsync(string refactoringId);

    /// <summary>
    /// Validate ontology changes before creating PR
    /// </summary>
    Task<bool> ValidateRefactoringAsync(string entityName);
}
