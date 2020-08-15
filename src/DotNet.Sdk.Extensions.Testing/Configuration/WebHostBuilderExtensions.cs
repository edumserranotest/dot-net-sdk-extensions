﻿using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace DotNet.Sdk.Extensions.Testing.Configuration
{
    public static class WebHostBuilderExtensions
    {
        /// <summary>
        /// Clears anyt loaded appsettings files by removing all JsonConfigurationSource
        /// from the <see cref="IWebHostBuilder"/> and adding only the provided appsettings files.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="appsettingsFilenames"></param>
        /// <returns>The <see cref="IWebHostBuilder"/> for chaining.</returns>
        public static IWebHostBuilder AddTestConfiguration(
            this IWebHostBuilder builder,
            params string[] appsettingsFilenames)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var options = new TestConfigurationOptions();
            return builder.AddTestConfiguration(options, appsettingsFilenames);
        }

        public static IWebHostBuilder AddTestConfiguration(
            this IWebHostBuilder builder,
            Action<TestConfigurationOptions> configureOptions,
            params string[] appsettingsFilenames)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            var options = new TestConfigurationOptions();
            configureOptions(options);
            return builder.AddTestConfiguration(options, appsettingsFilenames);
        }

        /*
         * Allows adding appsettings files to the webhost build used for tests.
         * Supports combination of appsetings files. I.E:  AddTestConfiguration("appsettings.Test.json")
         * will add both config files.
         * 
         * TestConfigurationOptions.AppSettingsDir is relative to the test project location
         * 
         */
        private static IWebHostBuilder AddTestConfiguration(
            this IWebHostBuilder builder,
            TestConfigurationOptions options,
            params string[] appsettingsFilenames)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (appsettingsFilenames == null) throw new ArgumentNullException(nameof(appsettingsFilenames));

            var projectDir = Path.Combine(Directory.GetCurrentDirectory(), options.AppSettingsDir);
            return builder.ConfigureAppConfiguration((context, config) =>
            {
                /*
                 * Remove json sources which are added by default when using WebApplicationFactory.
                 * Without doing this the configuration files loaded normally during the app might
                 * interfere with the tests.
                 */
                config.Sources
                    .OfType<JsonConfigurationSource>()
                    .ToList()
                    .ForEach(source => config.Sources.Remove(source));
                foreach (var appSettingFilename in appsettingsFilenames)
                {
                    var configPath = Path.Combine(projectDir, appSettingFilename);
                    config.AddJsonFile(configPath);
                }
            });
        }
    }
}
