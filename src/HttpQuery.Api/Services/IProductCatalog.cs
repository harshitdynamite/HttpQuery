using HttpQuery.Api.Models;

namespace HttpQuery.Api.Services;

public interface IProductCatalog
{
    ProductSearchResponse Search(ProductSearchRequest request);

    PriceLookupResponse LookupPrices(IReadOnlyList<int> ids);
}
