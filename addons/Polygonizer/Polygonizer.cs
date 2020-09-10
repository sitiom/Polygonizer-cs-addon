using Godot;
using System;
using System.Collections.Generic;

public class Polygonizer : Reference
{
	public double Resolution;
	public double Margin;

	private List<Vector2> _polygon = new List<Vector2>();
	private List<Vector2> _tracePoints = new List<Vector2>();

	public List<Vector2> Scan(Sprite sprite)
	{
		Image image = sprite.Texture.GetData();

		int frameWidth = image.GetWidth() / sprite.Hframes;
		int frameHeight = image.GetHeight() / sprite.Vframes;

		_polygon = new List<Vector2>();
		_tracePoints = new List<Vector2>();

		bool lastIncluded;
		image.Lock();
		// Top to bottom
		for (int x = (int)sprite.FrameCoords.x * frameWidth; x < (int)sprite.FrameCoords.x * frameWidth + frameWidth; x += (int)Resolution + 1)
		{
			lastIncluded = false;
			for (int y = (int)sprite.FrameCoords.y * frameHeight; y < (int)sprite.FrameCoords.y * frameHeight + frameHeight; y += (int)Resolution + 1)
			{
				if (image.GetPixel(x, y).a > .1)
				{
					if (!lastIncluded)
					{
						_tracePoints.Add(new Vector2(x, (float) (y - Resolution - Margin)));
						lastIncluded = true;
					}
				}
				else
				{
					if (lastIncluded)
					{
						_tracePoints.Add(new Vector2(x, (float) (y + Resolution + Margin)));
						lastIncluded = false;
					}
				}
			}

			if (lastIncluded)
			{
				_tracePoints.Add(new Vector2(x, (float) (image.GetHeight() + Resolution + Margin)));
			}
		}

		// Left to right
		for (int y = (int)sprite.FrameCoords.y * frameHeight; y < (int)sprite.FrameCoords.y * frameHeight + frameHeight; ++y)
		{
			lastIncluded = false;
			for (int x = (int)sprite.FrameCoords.x * frameWidth; x < (int)sprite.FrameCoords.x * frameWidth + frameWidth; ++x)
			{
				if (image.GetPixel(x, y).a > .1)
				{
					if (!lastIncluded)
					{
						_tracePoints.Add(new Vector2((float) (x - Resolution * 2 - Margin), y));
						lastIncluded = true;
					}
				}
				else
				{
					if (lastIncluded)
					{
						_tracePoints.Add(new Vector2((float) (x + Resolution + Margin), y));
						lastIncluded = false;
					}
				}

			}

			if (lastIncluded)
			{
				_tracePoints.Add(new Vector2((float) (image.GetWidth() + Resolution + Margin), y));
			}
		}
		image.Unlock();

		_tracePoints.Sort((p1, p2) =>
		{
			if (p1.x == p2.x)
			{
				if (p1.y < p2.y)
				{
					return 1;
				}

				return -1;
			}

			if (p1.x < p2.x)
			{
				return 1;
			}

			return -1;
		});

		MonotoneChain();
		return _polygon;
	}

	public bool IsLeftOfLine(Vector2 point, Vector2 a, Vector2 b)
	{
		return ((a.x - b.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x)) > 0;
	}

	public double DistanceToLine(Vector2 point, Vector2 a, Vector2 b)
	{
		return Math.Abs((a.x - b.x) * (a.y - point.y) - (a.x - point.x) * (b.y - a.y)) /
			   Math.Sqrt(Math.Pow(b.x - a.x, 2) + Math.Pow(b.y - a.y, 2));
	}

	public float Cross(Vector2 a, Vector2 b, Vector2 c)
	{
		return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
	}

	private void MonotoneChain()
	{
		_polygon = new List<Vector2>();

		if (_tracePoints.Count > 1)
		{
			int n = _tracePoints.Count;
			int k = 0;
			List<Vector2> hull = new List<Vector2>();
			for (int i = 0; i < n * 2 - 1; i++)
			{
				hull.Add(new Vector2());
			}

			for (int i = 0; i < n - 1; i++)
			{
				while (k >= 2 && Cross(hull[k - 2], hull[k - 1], _tracePoints[i]) <= 0)
				{
					k -= 1;
				}

				hull[k] = _tracePoints[i];
				k += 1;
			}

			int t = k + 1;
			for (int i = n - 2; i > 0; i--)
			{
				while (k >= t && Cross(hull[k - 2], hull[k - 1], _tracePoints[i]) <= 0)
				{
					k -= 1;
				}

				hull[k] = _tracePoints[i];
				k += 1;
			}

			for (int i = 0; i < k - 1; i++)
			{
				_polygon.Add(hull[i]);
			}
		}
		else
		{
			_polygon = _tracePoints;
		}
	}
}
