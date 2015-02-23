﻿using System;

namespace GitPushFilter
{
    public static class Extensions
    {
        public static bool SameAs(this string lhs, string rhs)
        {
            return string.Compare(lhs, rhs, true) == 0;
        }

        public static string DisplayHash(this byte[] b, int numDigits = 5)
        {
            return String.Join(String.Empty,
                    Array.ConvertAll(b, x => x.ToString("x2")))
                        .Substring(0, numDigits);
        }
    }
}