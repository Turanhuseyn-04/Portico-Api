using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Repository.Data;
using Service.Helpers.DTOs.Admin;

namespace Portico.Tests.Integration;

// The home page's slides (trending destinations) and video band come from the database, so admin changes show there.
[Collection(ApiCollection.Name)]
public class HomeContentTests : IClassFixture<PorticoApiFactory>
{
    private readonly PorticoApiFactory _factory;

    public HomeContentTests(PorticoApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PublicSlidesAndVideos_AreSeededFromTheOldHomePage()
    {
        var anonymous = _factory.CreateClient();

        var slides = await (await anonymous.GetAsync("/slides")).ReadAsync<SlideDto[]>();
        var videos = await (await anonymous.GetAsync("/videos")).ReadAsync<VideoDto[]>();

        Assert.Equal(new[] { "Rome", "Athens", "Paris", "Istanbul", "Venice", "Lisbon", "Kyoto", "Marrakech" }, slides.Take(8).Select(s => s.City));
        Assert.Equal("/assets/rome.jpg", slides[0].Image);
        Assert.Equal("/assets/videos/villas/14041585_1920_1080_25fps.mp4", videos[0].Src);
    }

    [Fact]
    public async Task AdminLists_ShowTheSameContentAsTheHomePage()
    {
        var anonymous = _factory.CreateClient();
        var boss = await _factory.SuperAdminAsync();

        var publicSlides = await (await anonymous.GetAsync("/slides")).ReadAsync<SlideDto[]>();
        var adminSlides = await (await boss.GetAsync("/admin/slides")).ReadAsync<SlideDto[]>();

        Assert.Equal(publicSlides.Select(s => s.Id), adminSlides.Select(s => s.Id));
    }

    [Fact]
    public async Task SlideAddedInAdmin_AppearsOnPublicListInItsOrder()
    {
        var boss = await _factory.SuperAdminAsync();
        var created = await (await boss.PostAsJsonAsync("/admin/slides", new { city = "Baku", country = "Azerbaijan", image = "/uploads/slides/baku.jpg", stays = 0, order = -1 }))
            .ReadAsync<SlideDto>();

        var slides = await (await _factory.CreateClient().GetAsync("/slides")).ReadAsync<SlideDto[]>();

        Assert.Equal(created.Id, slides[0].Id);
        await boss.DeleteAsync($"/admin/slides/{created.Id}");
    }

    [Fact]
    public async Task SlideWithExternalImage_IsRefused()
    {
        var boss = await _factory.SuperAdminAsync();
        var response = await boss.PostAsJsonAsync("/admin/slides", new { city = "X", country = "Y", image = "https://evil.example/x.jpg", stays = 0, order = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Seed_RunsOnce_SoDeletedSlidesStayDeleted()
    {
        var options = new DbContextOptionsBuilder<PorticoDbContext>().UseInMemoryDatabase("seed-" + Guid.NewGuid()).Options;
        await using (var db = new PorticoDbContext(options))
        {
            await CatalogSeed.EnsureAsync(db, NullLogger.Instance, default);
            Assert.Equal(8, await db.Slides.CountAsync());
            Assert.Equal(2, await db.Videos.CountAsync());
            Assert.Equal(1, await db.Slides.CountAsync(s => s.City == "Lisbon" && s.Stays > 0)); // stay count from the catalog

            db.Slides.RemoveRange(db.Slides);
            db.Videos.RemoveRange(db.Videos);
            await db.SaveChangesAsync();
        }

        await using (var db = new PorticoDbContext(options))
        {
            await CatalogSeed.EnsureAsync(db, NullLogger.Instance, default); // next start of the API
            Assert.Equal(0, await db.Slides.CountAsync());
            Assert.Equal(0, await db.Videos.CountAsync());
        }
    }
}
