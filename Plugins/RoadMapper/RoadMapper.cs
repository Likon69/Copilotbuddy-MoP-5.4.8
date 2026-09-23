using System;
using Styx.Plugins;

namespace RoadMapper;

internal class RoadMapper : HBPlugin
{
	public override string Name => "Road mapper";

	public override string Author => "MaiN";

	public override Version Version => new Version(0, 1);

	public override bool WantButton => true;

	public override string ButtonText => "Open";

	public override void Pulse()
	{
	}

	public override void OnButtonPress()
	{
		FormRoadMapper formRoadMapper = new FormRoadMapper();
		formRoadMapper.Show();
	}
}
