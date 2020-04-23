using System;
using System.Collections.Generic;
using UnityEngine;

struct BoundingBox
{
    public BoundingBox(float xMin, float xMax, float yMin, float yMax)
    {
        this.xMin = xMin;
        this.xMax = xMax;
        this.yMin = yMin;
        this.yMax = yMax;
    }

    public BoundingBox(float x, float y) : this(x, x, y, y) { }

    public BoundingBox(Vector2 point) : this(point.x, point.x, point.y, point.y) { }

    public BoundingBox(Rect rect) : this(rect.xMin, rect.xMax, rect.yMin, rect.yMax) { }

    public float xMin { get; }
    public float xMax { get; }
    public float yMin { get; }
    public float yMax { get; }

    public float width => xMax - xMin;

    public float height => yMax - yMin;

    public BoundingBox AddPoint(Vector2 point) => AddPoint(point.x, point.y);

    public BoundingBox AddPoint(float x, float y) => new BoundingBox(
        Math.Min(xMin, x),
        Math.Max(xMax, x),
        Math.Min(yMin, y),
        Math.Max(yMax, y)
        );

    public BoundingBox AddRect(Rect rect) => new BoundingBox(
        Math.Min(xMin, rect.xMin),
        Math.Max(xMax, rect.xMax),
        Math.Min(yMin, rect.yMin),
        Math.Max(yMax, rect.yMax)
        );
}
