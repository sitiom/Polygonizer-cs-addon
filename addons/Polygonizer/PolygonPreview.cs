#if TOOLS
using Godot;
using System.Collections.Generic;

[Tool]
public class PolygonPreview : Node2D
{
	private List<Vector2> _polygon = new List<Vector2>();

	public void SetPolygon(List<Vector2> polygon)
	{
		_polygon = polygon;
		Update();
	}

	public override void _Draw()
	{
		foreach (Vector2 coordinate in _polygon)
		{
			DrawCircle(coordinate, .3f, new Color(1, 0, 0));
		}

		Vector2 lastCoordinate = _polygon[0];

		if (_polygon.Count > 1)
		{
			for (int i = 1; i < _polygon.Count; i++)
			{
				Vector2 coordinate = _polygon[i];
				DrawLine(lastCoordinate, coordinate, new Color(1, 1, 1));
				lastCoordinate = coordinate;
			}
		}

		DrawLine(lastCoordinate, _polygon[0], new Color(1, 1, 1));
	}
}
#endif
