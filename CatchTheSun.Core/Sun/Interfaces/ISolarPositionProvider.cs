using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun.Interfaces;

public interface ISolarPositionProvider
{
    SunPosition GetPosition(SolarLocation location, DateTimeOffset instant);
}
