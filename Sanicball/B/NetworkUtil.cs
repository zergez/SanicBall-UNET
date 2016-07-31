using System.Text;

//using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Collections;
using System.Collections.Generic;

public static class NetworkUtil
{
	#region Match Request
	private static string kPackKeyDelimiter = "||";
	private static string kPackAttrDelimiter = "&&";

	public static void PackMatchRequest( CreateMatchRequest matchRequest )
	{
		StringBuilder packedAttributeBuilder = new StringBuilder();

		if( matchRequest.matchAttributes != null && matchRequest.matchAttributes.Count > 0 )
		{
			packedAttributeBuilder.Append( kPackAttrDelimiter );
		}

		foreach( KeyValuePair<string, long> attr in matchRequest.matchAttributes )
		{
			packedAttributeBuilder.Append( attr.Key );
			packedAttributeBuilder.Append( kPackKeyDelimiter );
			packedAttributeBuilder.Append( attr.Value );
			packedAttributeBuilder.Append( kPackAttrDelimiter );
		}

		matchRequest.name = matchRequest.name + packedAttributeBuilder.ToString();
	}

	public static void UnpackMatchDesc( MatchDesc matchDesc )
	{
		if( matchDesc.matchAttributes != null && matchDesc.matchAttributes.Count > 0 )
		{
			Debug.Log("Match Attributes have been implemented!, Stop using this!");
		}

		// if we have packed attributes, unpack them
		if( matchDesc.name.IndexOf(kPackAttrDelimiter) != -1 )
		{
			string[] packedAttributes = matchDesc.name.Split(new string[]{kPackAttrDelimiter},System.StringSplitOptions.RemoveEmptyEntries );

			// name is always first
			matchDesc.name = packedAttributes[0];

			matchDesc.matchAttributes = new Dictionary<string, long>();

			for( int i = 1; i < packedAttributes.Length; i++ )
			{
				string[] attrComponents = packedAttributes[i].Split( new string[]{ kPackKeyDelimiter }, System.StringSplitOptions.None );
				matchDesc.matchAttributes.Add( attrComponents[0], System.Convert.ToInt64( attrComponents[1] ) );
			}
		}
	}
	#endregion
}