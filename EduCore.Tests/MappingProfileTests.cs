using AutoMapper;
using EduCore.Mapping;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduCore.Tests;

public class MappingProfileTests
{
    [Fact]
    public void Configuration_is_valid()
    {
        var config = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
    }
}
