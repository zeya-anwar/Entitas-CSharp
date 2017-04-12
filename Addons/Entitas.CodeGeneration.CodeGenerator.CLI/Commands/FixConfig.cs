﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Entitas.Utils;

namespace Entitas.CodeGeneration.CodeGenerator.CLI {

    public static class FixConfig {

        public static void Run() {
            if(File.Exists(Preferences.configPath)) {
                var properties = Preferences.LoadProperties();
                var defaultProperties = new CodeGeneratorConfig().defaultProperties;
                var requiredKeys = defaultProperties.Keys.ToArray();

                Type[] types = null;
                string[] configurableKeys = null;
                var config = new CodeGeneratorConfig();
                config.Configure(properties);

                try {
                    types = CodeGeneratorUtil.LoadTypesFromCodeGeneratorAssemblies();
                    configurableKeys = Helper.GetConfigurableKeys(
                        CodeGeneratorUtil.GetUsed<ICodeGeneratorDataProvider>(types, config.dataProviders),
                        CodeGeneratorUtil.GetUsed<ICodeGenerator>(types, config.codeGenerators),
                        CodeGeneratorUtil.GetUsed<ICodeGenFilePostProcessor>(types, config.postProcessors)
                    );
                } catch(Exception ex) {
                    fixKeys(requiredKeys, null, defaultProperties, properties);
                    throw ex;
                }

                fixKeys(requiredKeys, configurableKeys, defaultProperties, properties);
                fixConfigurableKeys(configurableKeys, properties);
                fixPlugins(types, config, properties);

            } else {
                PrintNoConfig.Run();
            }
        }

        static void fixKeys(string[] requiredKeys, string[] configurableKeys, Dictionary<string, string> defaultProperties, Properties properties) {
            var requiredKeysWithConfigurable = requiredKeys.ToList().ToArray();

            if(configurableKeys != null) {
                requiredKeysWithConfigurable = requiredKeysWithConfigurable.Concat(configurableKeys).ToArray();
            }

            foreach(var key in Helper.GetMissingKeys(requiredKeys, properties)) {
                Helper.AddKey("Add missing key", key, defaultProperties[key], properties);
            }

            foreach(var key in Helper.GetUnusedKeys(requiredKeysWithConfigurable, properties)) {
                Helper.RemoveKey("Remove unused key", key, properties);
            }
        }

        static void fixConfigurableKeys(string[] configurableKeys, Properties properties) {
            foreach(var key in Helper.GetMissingConfigurableKeys(configurableKeys, properties)) {
                Helper.AddKey("Add missing key", key, string.Empty, properties);
            }
        }

        static void fixPlugins(Type[] types, CodeGeneratorConfig config, Properties properties) {
            var unavailableDataProviders = CodeGeneratorUtil.GetUnavailable<ICodeGeneratorDataProvider>(types, config.dataProviders);
            var unavailableCodeGenerators = CodeGeneratorUtil.GetUnavailable<ICodeGenerator>(types, config.codeGenerators);
            var unavailablePostProcessors = CodeGeneratorUtil.GetUnavailable<ICodeGenFilePostProcessor>(types, config.postProcessors);

            var availableDataProviders = CodeGeneratorUtil.GetAvailable<ICodeGeneratorDataProvider>(types, config.dataProviders);
            var availableCodeGenerators = CodeGeneratorUtil.GetAvailable<ICodeGenerator>(types, config.codeGenerators);
            var availablePostProcessors = CodeGeneratorUtil.GetAvailable<ICodeGenFilePostProcessor>(types, config.postProcessors);

            foreach(var dataProvider in unavailableDataProviders) {
                Helper.RemoveValue("Remove unavailable data provider", dataProvider, config.dataProviders,
                                   values => config.dataProviders = values, properties);
            }

            foreach(var codeGenerator in unavailableCodeGenerators) {
                Helper.RemoveValue("Remove unavailable code generator", codeGenerator, config.codeGenerators,
                                   values => config.codeGenerators = values, properties);
            }

            foreach(var postProcessor in unavailablePostProcessors) {
                Helper.RemoveValue("Remove unavailable post processor", postProcessor, config.postProcessors,
                                   values => config.postProcessors = values, properties);
            }

            foreach(var dataProvider in availableDataProviders) {
                Helper.AddValue("Add available data provider", dataProvider, config.dataProviders,
                                values => config.dataProviders = values, properties);
            }

            foreach(var codeGenerator in availableCodeGenerators) {
                Helper.AddValue("Add available code generator", codeGenerator, config.codeGenerators,
                                values => config.codeGenerators = values, properties);
            }

            foreach(var postProcessor in availablePostProcessors) {
                Helper.AddValue("Add available post processor", postProcessor, config.postProcessors,
                                values => config.postProcessors = values, properties);
            }
        }
    }
}