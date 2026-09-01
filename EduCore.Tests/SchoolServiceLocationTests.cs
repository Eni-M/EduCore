using AutoMapper;
using EduCore.Dtos.Location;
using EduCore.Dtos.School;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Mapping;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

public class SchoolServiceLocationTests
{
    private sealed class FakeSchoolRepository : ISchoolRepository
    {
        public School? Added;
        public Task<School?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<School?>(Added);
        public Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct) => Task.FromResult<School?>(Added);
        public Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task AddAsync(School entity) { Added = entity; return Task.CompletedTask; }
        public void Update(School entity) { Added = entity; }
        public void Remove(School entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeLocationRepository(City city, Zone zone) : ILocationRepository
    {
        public Task<IEnumerable<StateListDto>> GetStatesAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<StateListDto>>([]);
        public Task<Models.State?> GetStateAsync(Guid stateId, CancellationToken ct) => Task.FromResult<Models.State?>(null);
        public Task<bool> StateNameExistsAsync(string name, Guid? excludeStateId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> StateHasCitiesAsync(Guid stateId, CancellationToken ct) => Task.FromResult(false);
        public Task<IEnumerable<CityListDto>> GetCitiesAsync(Guid? stateId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<CityListDto>>([]);
        public Task<City?> GetCityAsync(Guid cityId, CancellationToken ct) =>
            Task.FromResult(cityId == city.Id ? city : null);
        public Task<CityListDto?> GetCityDtoAsync(Guid cityId, CancellationToken ct) => Task.FromResult<CityListDto?>(null);
        public Task<bool> CityNameExistsInStateAsync(Guid stateId, string name, Guid? excludeCityId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<bool> CityIsInUseAsync(Guid cityId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> CityHasZonesAsync(Guid cityId, CancellationToken ct) => Task.FromResult(false);
        public Task<IEnumerable<ZoneListDto>> GetZonesAsync(Guid? cityId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ZoneListDto>>([]);
        public Task<Zone?> GetZoneAsync(Guid zoneId, CancellationToken ct) =>
            Task.FromResult(zoneId == zone.Id ? zone : null);
        public Task<ZoneListDto?> GetZoneDtoAsync(Guid zoneId, CancellationToken ct) => Task.FromResult<ZoneListDto?>(null);
        public Task<bool> ZoneNameExistsInCityAsync(Guid cityId, string name, Guid? excludeZoneId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<bool> ZoneIsInUseAsync(Guid zoneId, CancellationToken ct) => Task.FromResult(false);
        public Task AddStateAsync(Models.State state) => Task.CompletedTask;
        public Task AddCityAsync(City city) => Task.CompletedTask;
        public Task AddZoneAsync(Zone zone) => Task.CompletedTask;
        public void RemoveState(Models.State state) { }
        public void RemoveCity(City city) { }
        public void RemoveZone(Zone zone) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static IMapper Mapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Creating_a_school_with_a_zone_from_a_different_city_throws_business_exception()
    {
        var cityA = new City { Id = Guid.NewGuid(), Name = "Prishtina" };
        var cityB = new City { Id = Guid.NewGuid(), Name = "Prizren" };
        var zoneInCityB = new Zone { Id = Guid.NewGuid(), Name = "Qendra", CityId = cityB.Id };

        var service = new SchoolService(
            new FakeSchoolRepository(),
            new FakeLocationRepository(cityA, zoneInCityB),
            Mapper());

        var dto = new SchoolCreateDto("New School", SchoolType.HighSchool, cityA.Id, zoneInCityB.Id);

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task Creating_a_school_with_a_zone_that_belongs_to_the_given_city_succeeds()
    {
        var city = new City { Id = Guid.NewGuid(), Name = "Prishtina" };
        var zone = new Zone { Id = Guid.NewGuid(), Name = "Qendra", CityId = city.Id };
        var repo = new FakeSchoolRepository();

        var service = new SchoolService(repo, new FakeLocationRepository(city, zone), Mapper());

        var dto = new SchoolCreateDto("New School", SchoolType.HighSchool, city.Id, zone.Id);

        await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(repo.Added);
        Assert.Equal(city.Id, repo.Added!.CityId);
        Assert.Equal(zone.Id, repo.Added!.ZoneId);
    }
}
