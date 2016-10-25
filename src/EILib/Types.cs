using System;
using System.Collections.Generic;
using System.Text;

namespace EILib
{
    public class Plot
    {
        public double X;
        public double Y;
        public double Z;

        public Plot()
        {
            X = Y = Z = 0.0;
        }

        public Plot(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Plot operator +(Plot a, Plot b)
        {
            return new Plot(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Plot operator -(Plot a, Plot b)
        {
            return new Plot(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
    }

    public class Quaternion
    {
        public double W;
        public double X;
        public double Y;
        public double Z;

        public Quaternion()
        {
            W = X = Y = Z = 0.0;
        }

        public Quaternion(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Quaternion operator +(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        public static Quaternion operator -(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }
    }
}
