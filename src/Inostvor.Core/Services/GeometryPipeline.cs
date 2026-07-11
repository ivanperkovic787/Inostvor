using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Core.Model.Validation;

namespace Inostvor.Core.Services;

/// <summary>Kompozicija build → classify → validate; čista funkcija bez stanja (Baseline §3).</summary>
public sealed class GeometryPipeline : IGeometryPipeline
{
    private readonly IContourBuilder _builder;
    private readonly IContourClassifier _classifier;
    private readonly IToolpathValidator _validator;

    public GeometryPipeline(IContourBuilder builder, IContourClassifier classifier, IToolpathValidator validator)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(classifier);
        ArgumentNullException.ThrowIfNull(validator);
        _builder = builder;
        _classifier = classifier;
        _validator = validator;
    }

    public GeometryPipelineResult Process(IReadOnlyList<ImportedEntity> entities, ContourBuildSettings settings)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(settings);

        var build = _builder.Build(entities, settings);
        var classified = _classifier.Classify(build.Contours);
        var report = _validator.Validate(new ValidationContext(classified, build.Joins, settings));

        return new GeometryPipelineResult(classified, build.Joins, report);
    }
}
