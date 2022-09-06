using System;

namespace AutoRobotControl
	{
	public interface FeatureMatch
		{
		Room.feature_match MatchKinect(NavData.feature f,params object[] obj);
		}
	}
