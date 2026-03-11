namespace Baseline.Core;

/// <summary>
/// Default implementation of IPageIdentityFactory for v3.
/// </summary>
public class PageIdentityFactory : IPageIdentityFactory
{
    /// <inheritdoc />
    public PageIdentity Create(int contentItemId, string name, string path)
    {
        return new PageIdentity
        {
            ContentID = contentItemId,
            Name = name,
            Path = path
        };
    }
}
