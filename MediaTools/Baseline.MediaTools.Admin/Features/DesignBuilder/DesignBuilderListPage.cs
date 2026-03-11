using Baseline.MediaTools.Admin.Features.DesignBuilder;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(DesignBuilderListPage),
    parentType: typeof(DesignBuilderApplicationPage),
    slug: "designs",
    name: "My Designs",
    templateName: "@baseline/media-tools/DesignListLayout",
    order: 0,
    Icon = Icons.Layout)]

namespace Baseline.MediaTools.Admin.Features.DesignBuilder;

public class DesignBuilderListPage : Page<DesignListClientProperties>
{
    public override async Task<DesignListClientProperties> ConfigureTemplateProperties(
        DesignListClientProperties properties)
    {
        properties.Designs = await GetSavedDesigns();
        return properties;
    }

    [PageCommand(CommandName = "DELETE_DESIGN")]
    public async Task<ICommandResponse> DeleteDesign(DeleteDesignArgs args)
    {
        return Response().AddSuccessMessage("Design deleted.");
    }

    [PageCommand(CommandName = "DUPLICATE_DESIGN")]
    public async Task<ICommandResponse> DuplicateDesign(DuplicateDesignArgs args)
    {
        return Response().AddSuccessMessage("Design duplicated.");
    }

    private Task<IEnumerable<DesignSummary>> GetSavedDesigns() =>
        Task.FromResult<IEnumerable<DesignSummary>>([]);
}

public class DesignListClientProperties : TemplateClientProperties
{
    public IEnumerable<DesignSummary> Designs { get; set; } = [];
}

public record DuplicateDesignArgs(int DesignId);
