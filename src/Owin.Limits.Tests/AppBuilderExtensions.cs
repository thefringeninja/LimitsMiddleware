﻿namespace Owin
{
    using System;
    using Owin.Limits;

    internal static class AppBuilderExtensions
    {
        internal static Action<MidFunc> Use(this IAppBuilder builder)
        {
            return middleware => builder.Use(middleware);
        }

        internal static IAppBuilder Use(this Action<MidFunc> middleware, IAppBuilder builder)
        {
            return builder;
        }
    }
}