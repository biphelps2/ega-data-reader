using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Spl.Core.Monogame;

// Calculate visible area from a position
// Copyright 2012 Red Blob Games https://www.redblobgames.com/articles/visibility/
// Modified by biphelps.
// License: Apache v2

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at https://www.apache.org/licenses/LICENSE-2.0
namespace Spl.Core
{
    public class LightVisibilityCalculator
    {
        public class Endpoint
        {
            public float X { get; init; }
            public float Y { get; init; }

            public bool IsBegin { get; set; }
            public Segment? SegmentReference { get; set; }
            public float Angle { get; set; }

            public Vector2 ToVector2()
            {
                return new Vector2(X, Y);
            }
        }

        public class Segment
        {
            public Endpoint P1 { get; }
            public Endpoint P2 { get; }

            public Segment(Endpoint p1, Endpoint p2)
            {
                P1 = p1;
                P2 = p2;
            }
        }

        // These are currently 'open' line segments, sorted so that the nearest
        // segment is first. It's used only during the sweep algorithm, and exposed
        // as a public field here so that the demo can display it.
        private List<Segment> OpenSegments { get; set; }
        private List<Segment> Segments { get; }
        private List<Endpoint> Endpoints { get; }
        private Vector2 LightPos { get; set; }

        public LightVisibilityCalculator()
        {
            Segments = new List<Segment>();
            Endpoints = new List<Endpoint>();
            OpenSegments = new List<Segment>();

            LightPos = Vector2.Zero;
        }

        private void LoadBoundarySegments(int x, int y, int width, int height)
        {
            var edgePolygon = new Polygon(new[]
            {
                new Vector2(x, y),
                new Vector2(x + width, y),
                new Vector2(x + width, y + height),
                new Vector2(x, y + height)
            });

            foreach (var l in edgePolygon.Lines)
            {
                AddSegment(l.Item1, l.Item2);
            }
        }

        public void LoadMap(int x, int y, int width, int height, (Vector2, Vector2)[] sectionParts)
        {
            Segments.Clear();
            Endpoints.Clear();

            LoadBoundarySegments(x, y, width, height);

            foreach (var b in sectionParts)
            {
                AddSegment(b.Item1, b.Item2);
            }
        }

        // Add a segment, where the first point shows up in the
        // visualization but the second one does not. (Every endpoint is
        // part of two segments, but we want to only show them once.)
        private void AddSegment(Vector2 v1, Vector2 v2)
        {
            var p1 = new Endpoint
            {
                X = v1.X,
                Y = v1.Y
            };
            var p2 = new Endpoint
            {
                X = v2.X,
                Y = v2.Y
            };
            var s = new Segment(p1, p2);
            p1.SegmentReference = s;
            p2.SegmentReference = s;

            Segments.Add(s);
            Endpoints.Add(p1);
            Endpoints.Add(p2);
        }

        public void SetLightLocation(Vector2 pos)
        {
            LightPos = pos;

            foreach (var s in Segments)
            {
                // NOTE: future optimization: we could record the quadrant
                // and the y/x or x/y ratio, and sort by (quadrant,
                // ratio), instead of calling atan2. See
                // <https://github.com/mikolalysenko/compare-slope> for a
                // library that does this. Alternatively, calculate the
                // angles and use bucket sort to get an O(N) sort.
                s.P1.Angle = (float)Math.Atan2(s.P1.Y - LightPos.Y, s.P1.X - LightPos.X);
                s.P2.Angle = (float)Math.Atan2(s.P2.Y - LightPos.Y, s.P2.X - LightPos.X);

                // If we do a circular sweep, IsBegin marks which point we would see first.
                var dAngle = s.P2.Angle - s.P1.Angle;
                if (dAngle <= -Math.PI)
                {
                    dAngle += (float)(2 * Math.PI);
                }
                if (dAngle > Math.PI)
                {
                    dAngle -= (float)(2 * Math.PI);
                }
                s.P1.IsBegin = (dAngle > 0.0);
                s.P2.IsBegin = !s.P1.IsBegin;
            }
        }

        // TODO where is this used?
        private bool LeftOf(Segment s, Vector2 p)
        {
            // This is based on a 3d cross product, but we don't need to
            // use z coordinate inputs (they're 0), and we only need the
            // sign.
            var cross = (s.P2.X - s.P1.X) * (p.Y - s.P1.Y)
                        - (s.P2.Y - s.P1.Y) * (p.X - s.P1.X);
            return cross < 0;
            // Also note that this is the naive version of the test and
            // isn't numerically robust. See
            // <https://github.com/mikolalysenko/robust-arithmetic> for a
            // demo of how this fails when a point is very close to the
            // line.
        }

        private bool SegmentInFrontOf(Segment a, Segment b, Vector2 relativeTo)
        {
            // NOTE: we slightly shorten the segments so that
            // intersections of the endpoints (common) don't count as
            // intersections in this algorithm
            var a1 = LeftOf(a, Vector2.Lerp(b.P1.ToVector2(), b.P2.ToVector2(), 0.01f));
            var a2 = LeftOf(a, Vector2.Lerp(b.P2.ToVector2(), b.P1.ToVector2(), 0.01f));
            var a3 = LeftOf(a, relativeTo);
            var b1 = LeftOf(b, Vector2.Lerp(a.P1.ToVector2(), a.P2.ToVector2(), 0.01f));
            var b2 = LeftOf(b, Vector2.Lerp(a.P2.ToVector2(), a.P1.ToVector2(), 0.01f));
            var b3 = LeftOf(b, relativeTo);

            // NOTE: this algorithm is probably worthy of a short article
            // but for now, draw it on paper to see how it works. Consider
            // the line A1-A2. If both B1 and B2 are on one side and
            // relativeTo is on the other side, then A is in between the
            // viewer and B. We can do the same with B1-B2: if A1 and A2
            // are on one side, and relativeTo is on the other side, then
            // B is in between the viewer and A.
            if (b1 == b2 && b2 != b3)
            {
                return true;
            }
            if (a1 == a2 && a2 == a3)
            {
                return true;
            }
            if (a1 == a2 && a2 != a3)
            {
                return false;
            }
            if (b1 == b2 && b2 == b3)
            {
                return false;
            }

            // If A1 != A2 and B1 != B2 then we have an intersection.
            // Expose it for the GUI to show a message. A more robust
            // implementation would split segments at intersections so
            // that part of the segment is in front and part is behind.
            return false;

            // NOTE: previous implementation was a.d < b.d. That's simpler
            // but trouble when the segments are of dissimilar sizes. If
            // you're on a grid and the segments are similarly sized, then
            // using distance will be a simpler and faster implementation.
        }

        public List<Vector2> Sweep(float maxAngle = 999f)
        {
            var output = new List<Vector2>();
            var sortedEndpoints = Endpoints.OrderBy(e => e.Angle).ThenBy(e => e.IsBegin).ToArray();

            OpenSegments.Clear();
            var beginAngle = 0f;

            // At the beginning of the sweep we want to know which
            // segments are active. The simplest way to do this is to make
            // a pass collecting the segments, and make another pass to
            // both collect and process them. However it would be more
            // efficient to go through all the segments, figure out which
            // ones intersect the initial sweep line, and then sort them.
            for (var i = 0; i < 2; i++)
            {
                foreach (var p in sortedEndpoints)
                {
                    Debug.Assert(p.SegmentReference != null, "p.SegmentReference != null");
                    if (i == 1 && p.Angle > maxAngle)
                    {
                        // Early exit for the visualization to show the sweep process
                        break;
                    }

                    var currentOld = OpenSegments.FirstOrDefault();

                    if (p.IsBegin)
                    {
                        // Insert into the right place in the list
                        var insertAt = OpenSegments
                            .TakeWhile(s => SegmentInFrontOf(p.SegmentReference, s, LightPos))
                            .Count();

                        if (insertAt == OpenSegments.Count)
                        {
                            OpenSegments.Add(p.SegmentReference);
                        }
                        else
                        {
                            OpenSegments.Insert(insertAt, p.SegmentReference);
                        }
                    }
                    else
                    {
                        OpenSegments.Remove(p.SegmentReference);
                    }

                    var currentNew = OpenSegments.FirstOrDefault();
                    if (currentOld != currentNew)
                    {
                        if (i == 1)
                        {
                            var tri = AddTriangle(beginAngle, p.Angle, currentOld);
                            output.AddRange(new[] { tri.Item1, tri.Item2 });
                        }
                        beginAngle = p.Angle;
                    }
                }
            }

            return output;
        }

        private Vector2 LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            // NOTE: This will return NaN for / 0 when lines are parallel.

            // From http://paulbourke.net/geometry/lineline2d/
            var s = ((p4.X - p3.X) * (p1.Y - p3.Y) - (p4.Y - p3.Y) * (p1.X - p3.X))
                    / ((p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y));
            return new Vector2(p1.X + s * (p2.X - p1.X), p1.Y + s * (p2.Y - p1.Y));
        }

        private (Vector2, Vector2) AddTriangle(float angle1, float angle2, Segment? segment)
        {
            var p1 = LightPos;
            var p2 = new Vector2(LightPos.X + (float)Math.Cos(angle1), LightPos.Y + (float)Math.Sin(angle1));
            var p3 = new Vector2(0.0f, 0.0f);
            var p4 = new Vector2(0.0f, 0.0f);

            if (segment != null)
            {
                // Stop the triangle at the intersecting segment
                p3.X = segment.P1.X;
                p3.Y = segment.P1.Y;
                p4.X = segment.P2.X;
                p4.Y = segment.P2.Y;
            }
            else
            {
                // Stop the triangle at a fixed distance; this probably is
                // not what we want, but it never gets used in the demo
                p3.X = LightPos.X + (float)Math.Cos(angle1) * 500;
                p3.Y = LightPos.Y + (float)Math.Sin(angle1) * 500;
                p4.X = LightPos.X + (float)Math.Cos(angle2) * 500;
                p4.Y = LightPos.Y + (float)Math.Sin(angle2) * 500;
            }

            var pBegin = LineIntersection(p3, p4, p1, p2);

            p2.X = LightPos.X + (float)Math.Cos(angle2);
            p2.Y = LightPos.Y + (float)Math.Sin(angle2);
            var pEnd = LineIntersection(p3, p4, p1, p2);

            return (pBegin, pEnd);
        }
    }
}
