
using ServiceClientSearch.Application.Services;
using ServiceClientSearch.Domain.Models;

namespace ServiceClientSearch.Tests;

[TestClass]
public sealed class ClientSearchServiceTests
{
    [TestMethod]
    public async Task SearchAsync_UsesDefaults_WhenPageAndPageSizeAreMissing()
    {
        var repository = new FakeRepository();
        var sut = new Application.Services.IClientSearchService(repository);

        await sut.SearchAsync(new ClientSearchRequestModel(
            KlientId: null,
            Cnum: null,
            Ico: null,
            RodneCislo: null,
            Meno: null,
            Priezvisko: null,
            Mesto: null,
            Psc: null,
            TelCislo: null,
            Email: null,
            Ifo: null,
            Page: null,
            PageSize: null),
            CancellationToken.None);

        Assert.IsNotNull(repository.LastCriteria);
        Assert.AreEqual(Application.Services.IClientSearchService.DefaultPage, repository.LastCriteria.Page);
        Assert.AreEqual(Application.Services.IClientSearchService.DefaultPageSize, repository.LastCriteria.PageSize);
    }

    [TestMethod]
    public async Task SearchAsync_ClampsPageSizeToMaximum()
    {
        var repository = new FakeRepository();
        var sut = new Application.Services.IClientSearchService(repository);

        await sut.SearchAsync(new ClientSearchRequestModel(
            KlientId: null,
            Cnum: null,
            Ico: null,
            RodneCislo: null,
            Meno: null,
            Priezvisko: null,
            Mesto: null,
            Psc: null,
            TelCislo: null,
            Email: null,
            Ifo: null,
            Page: 1,
            PageSize: 1000),
            CancellationToken.None);

        Assert.IsNotNull(repository.LastCriteria);
        Assert.AreEqual(Application.Services.IClientSearchService.MaxPageSize, repository.LastCriteria.PageSize);
    }

    [TestMethod]
    public async Task SearchAsync_NormalizesTextValues()
    {
        var repository = new FakeRepository();
        var sut = new Application.Services.IClientSearchService(repository);

        await sut.SearchAsync(new ClientSearchRequestModel(
            KlientId: "  12345  ",
            Cnum: "   ",
            Ico: " 999 ",
            RodneCislo: null,
            Meno: "  Jan  ",
            Priezvisko: " Novak ",
            Mesto: " Bratislava ",
            Psc: " 82101 ",
            TelCislo: " 0903123456 ",
            Email: "  test@example.com ",
            Ifo: "  IF123 ",
            Page: 2,
            PageSize: 10),
            CancellationToken.None);

        Assert.IsNotNull(repository.LastCriteria);
        Assert.AreEqual("12345", repository.LastCriteria.KlientId);
        Assert.IsNull(repository.LastCriteria.Cnum);
        Assert.AreEqual("999", repository.LastCriteria.Ico);
        Assert.AreEqual("Jan", repository.LastCriteria.Meno);
        Assert.AreEqual("Novak", repository.LastCriteria.Priezvisko);
        Assert.AreEqual("Bratislava", repository.LastCriteria.Mesto);
        Assert.AreEqual("82101", repository.LastCriteria.Psc);
        Assert.AreEqual("0903123456", repository.LastCriteria.TelCislo);
        Assert.AreEqual("test@example.com", repository.LastCriteria.Email);
        Assert.AreEqual("IF123", repository.LastCriteria.Ifo);
        Assert.AreEqual(2, repository.LastCriteria.Page);
        Assert.AreEqual(10, repository.LastCriteria.PageSize);
    }

    [TestMethod]
    public async Task SearchAsync_ThrowsOnNullRequest()
    {
        var repository = new FakeRepository();
        var sut = new Application.Services.IClientSearchService(repository);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => sut.SearchAsync(null!, CancellationToken.None));
    }

    [TestMethod]
    public async Task SearchAsync()
    {
        var repository = new FakeRepository();
        var sut = new Application.Services.IClientSearchService(repository);

        var result = await sut.SearchAsync(new ClientSearchRequestModel(
            KlientId: null,
            Cnum: null,
            Ico: null,
            RodneCislo: "020331",
            Meno: null,
            Priezvisko: null,
            Mesto: null,
            Psc: null,
            TelCislo: null,
            Email: null,
            Ifo: null,
            Page: 1,
            PageSize: 1000),
            CancellationToken.None);

        Assert.IsNotNull(repository.LastCriteria);
        Assert.AreEqual(Application.Services.IClientSearchService.MaxPageSize, repository.LastCriteria.PageSize);
    }

    private sealed class FakeRepository : IClientSearchRepository
    {
        public ClientSearchCriteria? LastCriteria { get; private set; }

        public Task<ClientSearchResultPage> SearchAsync(ClientSearchCriteria criteria, CancellationToken cancellationToken)
        {
            LastCriteria = criteria;

            return Task.FromResult(new ClientSearchResultPage(
                Items: Array.Empty<ClientSearchItem>(),
                TotalCount: 0,
                Page: criteria.Page,
                PageSize: criteria.PageSize,
                HasMore: false));
        }
    }
}
