﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit.Sdk;

namespace NBean.Tests {

    static class AssertExtensions {

        public static void Equivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
            if(!new HashSet<T>(actual).SetEquals(expected))
                throw new EqualException(expected, actual);
        }

        public static void WithCulture(string culture, Action action) {
            var backup = CultureInfo.CurrentCulture;
            SetThreadCulture(new CultureInfo(culture));
            try {
                action();
            } finally {
                SetThreadCulture(backup);
            }
        }

        static void SetThreadCulture(CultureInfo value) {
            CultureInfo.CurrentCulture = value;
        }

    }

}
