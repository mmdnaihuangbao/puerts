/*
* Tencent is pleased to support the open source community by making Puerts available.
* Copyright (C) 2020 Tencent.  All rights reserved.
* Puerts is licensed under the BSD 3-Clause License, except for the third-party components listed in the file 'LICENSE' which may be subject to their corresponding license terms.
* This file is subject to the terms and conditions defined in file 'LICENSE', which is part of this source code package.
*/

using System.Collections.Generic;
using UnrealBuildTool;
using System.IO;
using System.Reflection;

public class JsEnv : ModuleRules
{    
    enum SupportedV8Versions
    {
        VDeprecated, // for 4.24 or blow only
        V8_4_371_19,
        V9_4_146_24,
        V11_8_172
    }

    private SupportedV8Versions UseV8Version = 
#if UE_4_25_OR_LATER
        SupportedV8Versions.V9_4_146_24;
#else
        SupportedV8Versions.VDeprecated;
#endif

    private bool UseNodejs = false;

    private bool Node16 = true;

    private bool UseQuickjs = false;

    private bool QjsNamespaceSuffix = false;

    private bool WithFFI = false;
    
    private bool ForceStaticLibInEditor = false;

    private bool ThreadSafe = false;

    private bool FTextAsString = true;
    
    private bool bEditorSuffix = true;

    // v8 9.4+
    private bool SingleThreaded = false;
    
    public static bool WithSourceControl = false;

    public bool WithByteCode = false;

    private bool WithWebsocket = false;
    
    public JsEnv(ReadOnlyTargetRules Target) : base(Target)
    {
        PublicDefinitions.Add("USING_IN_UNREAL_ENGINE");
        //PublicDefinitions.Add("WITH_V8_FAST_CALL");
        
        PublicDefinitions.Add("TS_BLUEPRINT_PATH=\"/Blueprints/TypeScript/\"");
        
        PublicDefinitions.Add(ThreadSafe ? "THREAD_SAFE" : "NOT_THREAD_SAFE");

        if (bEditorSuffix)
        {
            PublicDefinitions.Add("PUERTS_WITH_EDITOR_SUFFIX");
        }

        if (WithWebsocket)
        {
            PublicDefinitions.Add("WITH_WEBSOCKET");
            PublicDefinitions.Add("WITH_WEBSOCKET_SSL");
            PublicDependencyModuleNames.Add("OpenSSL");
        }

        ShadowVariableWarningLevel = WarningLevel.Warning;

        if (!FTextAsString)
        {
            PublicDefinitions.Add("PUERTS_FTEXT_AS_OBJECT");
        }

        PublicDependencyModuleNames.AddRange(new string[]
        {
            "Core", "CoreUObject", "Engine", "ParamDefaultValueMetas", "UMG"
        });

        if (Target.bBuildEditor)
        {
            PublicDependencyModuleNames.AddRange(new string[] { "DirectoryWatcher", });
        }

        bEnableExceptions = true;
        var ContextField = GetType().GetField("Context", BindingFlags.Instance | BindingFlags.NonPublic);
        if (ContextField != null)
        {
            var bCanHotReloadField = ContextField.FieldType.GetField("bCanHotReload", BindingFlags.Instance | BindingFlags.Public);
            if (bCanHotReloadField != null)
            {
                bCanHotReloadField.SetValue(ContextField.GetValue(this), false);
            }
        }

        bool bForceAllUFunctionInCPP = false;
        if (bForceAllUFunctionInCPP)
        {
            PublicDefinitions.Add("PUERTS_FORCE_CPP_UFUNCTION=1");
        }
        else
        {
            PublicDefinitions.Add("PUERTS_FORCE_CPP_UFUNCTION=0");
        }

        bool bKeepUObjectReference = true;
        if(bKeepUObjectReference)
        {
            PublicDefinitions.Add("PUERTS_KEEP_UOBJECT_REFERENCE=1");
        }
        else
        {
            PublicDefinitions.Add("PUERTS_KEEP_UOBJECT_REFERENCE=0");
        }

        bool UseWasm = false;
        if (UseWasm)
        {
            PublicDefinitions.Add("USE_WASM3=1");
        }
        else
        {
            PublicDefinitions.Add("USE_WASM3=0");
        }
        bool OverrideWebAssembly = false;
        if (OverrideWebAssembly)
        {
            PublicDefinitions.Add("WASM3_OVERRIDE_WEBASSEMBLY=1");
        }
        else
        {
            PublicDefinitions.Add("WASM3_OVERRIDE_WEBASSEMBLY=0");
        }
        PublicDependencyModuleNames.AddRange(new string[]
            {
                "WasmCore", "Json"
            });

        if (UseNodejs)
        {
            ThirdPartyNodejs(Target);
        }
        else if (UseQuickjs)
        {
            ForceStaticLibInEditor = true;
            ThirdPartyQJS(Target);
        }
        else if (UseV8Version > SupportedV8Versions.VDeprecated)
        {
            ThirdParty(Target);
        }
        else
        {
            OldThirdParty(Target);
        }
        
        if (WithFFI) AddFFI(Target);

        string coreJSPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "Content"));
        string destDirName = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "..", "..", "Content"));
        DirectoryCopy(coreJSPath, destDirName, true);

        // 每次build时拷贝一些手写的.d.ts到Typing目录以同步更新
        string srcDtsDirName  = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "Typing"));
        string dstDtsDirName = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "..", "..", "Typing"));
        DirectoryCopy(srcDtsDirName, dstDtsDirName, true);

    }

    void OldThirdParty(ReadOnlyTargetRules Target)
    {
        string LibraryPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "v8_for_ue424_or_below", "Lib"));
        if (Target.Platform == UnrealTargetPlatform.Win64)
        {
            //if (Target.bBuildEditor)
            //{
            //    WinDll(Path.Combine(LibraryPath, "V8"));
            //}
            //else
            {
                string V8LibraryPath = Path.Combine(LibraryPath, "Win64");

                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "encoding.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "inspector.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "inspector_string_conversions.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_base_without_compiler_0.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_base_without_compiler_1.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_compiler.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_external_snapshot.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_libbase.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_libplatform.lib"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_libsampler.lib"));
            }
        }
        else if (Target.Platform == UnrealTargetPlatform.Android)
        {
            if (Target.Version.MajorVersion == 4 && Target.Version.MinorVersion >= 25)
            {
                // for arm7
                string V8LibraryPath = Path.Combine(LibraryPath, "Android", "armeabi-v7a", "8.4.371.19");
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libwee8.a"));
                // for arm64
                V8LibraryPath = Path.Combine(LibraryPath, "V8", "Android", "arm64-v8a", "8.4.371.19");
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libwee8.a"));
            }
            else if (Target.Version.MajorVersion == 4 && Target.Version.MinorVersion < 25 && Target.Version.MinorVersion >= 22)
            {
                // for arm7
                string V8LibraryPath = Path.Combine(LibraryPath, "Android", "armeabi-v7a", "7.4.288");
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libinspector.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_base.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_external_snapshot.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libbase.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libplatform.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libsampler.a"));
                // for arm64
                V8LibraryPath = Path.Combine(LibraryPath, "Android", "arm64-v8a", "7.4.288");
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libinspector.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_base.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_external_snapshot.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libbase.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libplatform.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libsampler.a"));
            } 
#if !UE_4_22_OR_LATER
            else if (Target.Version.MajorVersion == 4 && Target.Version.MinorVersion < 22) 
            {
                string V8LibraryPath = Path.Combine(LibraryPath, "Android", "armeabi-v7a", "7.4.288");
                PublicLibraryPaths.Add(V8LibraryPath);
                V8LibraryPath = Path.Combine(LibraryPath, "Android", "arm64-v8a", "7.4.288");
                PublicLibraryPaths.Add(V8LibraryPath);
                PublicAdditionalLibraries.Add("inspector");
                PublicAdditionalLibraries.Add("v8_base");
                PublicAdditionalLibraries.Add("v8_external_snapshot");
                PublicAdditionalLibraries.Add("v8_libbase");
                PublicAdditionalLibraries.Add("v8_libplatform");
                PublicAdditionalLibraries.Add("v8_libsampler");
            }
#endif
        }
        else if (Target.Platform == UnrealTargetPlatform.Mac)
        {
            // PublicFrameworks.AddRange(new string[] { "WebKit",  "JavaScriptCore" });
            PublicFrameworks.AddRange(new string[] { "WebKit" });
            string V8LibraryPath = Path.Combine(LibraryPath, "macOS");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libbindings.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libencoding.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libinspector.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libinspector_string_conversions.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libtorque_base.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libtorque_generated_definitions.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libtorque_generated_initializers.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_base_without_compiler.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_compiler.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_external_snapshot.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_init.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_initializers.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libbase.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libplatform.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libsampler.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_nosnapshot.a"));
        }
        else if (Target.Platform == UnrealTargetPlatform.IOS)
        {
            PublicFrameworks.AddRange(new string[] { "WebKit" });
            string V8LibraryPath = Path.Combine(LibraryPath, "iOS", "arm64");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libbindings.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libencoding.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libinspector.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libinspector_string_conversions.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libtorque_generated_definitions.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_base_without_compiler.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_compiler.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_external_snapshot.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libbase.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libplatform.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libsampler.a"));

            //PublicAdditionalLibraries.Add(Path.Combine(Path.Combine(LibraryPath, "ffi", "iOS"), "libffi.a"));
        }
        else if (Target.Platform == UnrealTargetPlatform.Linux)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Linux");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libwee8.a"));
        }

        string V8HeaderPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "v8_for_ue424_or_below", "Inc"));
        // External headers
        if (Target.Platform == UnrealTargetPlatform.Android)
        {
            if (Target.Version.MajorVersion == 4 && Target.Version.MinorVersion >= 25)
            {
                PublicIncludePaths.AddRange(new string[] { Path.Combine(V8HeaderPath, "8.4.371.19") });
            }
            else if (Target.Version.MajorVersion == 4 && Target.Version.MinorVersion < 25)
            {
                PublicIncludePaths.AddRange(new string[] { Path.Combine(V8HeaderPath, "7.4.288") });
            }
        }
        //else if (Target.bBuildEditor && Target.Platform == UnrealTargetPlatform.Win64)
        //{
        //    PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "8.4.371.19") });
        //}
        else if (Target.Platform == UnrealTargetPlatform.Win64 ||
            Target.Platform == UnrealTargetPlatform.IOS ||
            Target.Platform == UnrealTargetPlatform.Mac ||
            Target.Platform == UnrealTargetPlatform.Linux)
        {
            PublicIncludePaths.AddRange(new string[] { Path.Combine(V8HeaderPath, "7.7.299") });
        }
        string HeaderPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "Include"));
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "websocketpp") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "asio") });
    }

    void AddFFI(ReadOnlyTargetRules Target)
    {
        string HeaderPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "Include"));
        string LibraryPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "Library"));
        if (Target.Platform == UnrealTargetPlatform.Win64)
        {
            PublicIncludePaths.AddRange(new string[] {Path.Combine(HeaderPath, "ffi", "Win64")});
            PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "ffi", "Win64", "ffi.lib"));
        }
        else if (Target.Platform == UnrealTargetPlatform.Mac)
        {
            PublicIncludePaths.AddRange(new string[] {Path.Combine(HeaderPath, "ffi", "macOS")});
            PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "ffi", "macOS", "libffi.a"));
        }
        else if (Target.Platform == UnrealTargetPlatform.IOS)
        {
            PublicIncludePaths.AddRange(new string[] {Path.Combine(HeaderPath, "ffi", "iOS")});
            PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "ffi", "iOS", "libffi.a"));
        }
        else if (Target.Platform == UnrealTargetPlatform.Android)
        {
            PublicIncludePaths.AddRange(new string[] {Path.Combine(HeaderPath, "ffi", "Android")});
            PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "ffi", "Android", "armeabi-v7a", "libffi.a"));
            PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "ffi", "Android", "arm64-v8a", "libffi.a"));
        }

        PrivateDefinitions.Add("WITH_FFI");
    }

    void AddRuntimeDependencies(string[] DllNames, string LibraryPath, bool Delay)
    {
        foreach (var DllName in DllNames)
        {
            if(Delay) PublicDelayLoadDLLs.Add(DllName);
            var DllPath = Path.Combine(LibraryPath, DllName);
            var DestDllPath = Path.Combine("$(BinaryOutputDir)", DllName);
            RuntimeDependencies.Add(DestDllPath, DllPath, StagedFileType.NonUFS);
        }
    }

    void WinDll(string LibraryPath)
    {
        string V8LibraryPath = Path.Combine(LibraryPath, "Win64DLL");
        PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8.dll.lib"));
        PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "v8_libplatform.dll.lib"));

        List<string> deps = new List<string> {
            "v8.dll",
            "v8_libplatform.dll",
            "v8_libbase.dll"
        };
        deps.Add(UseV8Version == SupportedV8Versions.V11_8_172 ? "third_party_zlib.dll" : "zlib.dll");

        AddRuntimeDependencies(deps.ToArray(), V8LibraryPath, false);
    }
    
    void MacDylib(string LibraryPath)
    {
        PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "libv8.dylib"));
        PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "libv8_libplatform.dylib"));
        PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "libv8_libbase.dylib"));
        PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "libchrome_zlib.dylib"));
        if (UseV8Version == SupportedV8Versions.V11_8_172)
        {
            PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "libthird_party_abseil-cpp_absl.dylib"));
        }
    }

    void ThirdParty(ReadOnlyTargetRules Target)
    {
        if (SingleThreaded)
        {
            PrivateDefinitions.Add("USING_SINGLE_THREAD_PLATFORM");
        }

        if (WithByteCode)
        {
            PrivateDefinitions.Add("WITH_V8_BYTECODE");
        }

        string v8LibSuffix = "";
        
        if (UseV8Version == SupportedV8Versions.V8_4_371_19)
        {
            if(Directory.Exists(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "v8_8.4.371.19")))
            {
                v8LibSuffix = "_8.4.371.19";
            }
        }
        else if (UseV8Version == SupportedV8Versions.V9_4_146_24)
        {
            v8LibSuffix = "_9.4.146.24";
        }
        else if (UseV8Version == SupportedV8Versions.V11_8_172)
        {
#if !UE_5_0_OR_LATER
            CppStandard = CppStandardVersion.Cpp17;
#endif
            v8LibSuffix = "_11.8.172";
        }
        //Add header
        string HeaderPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "Include"));
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "websocketpp") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "asio") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "v8" + v8LibSuffix, "Inc") });

        string LibraryPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "v8" + v8LibSuffix, "Lib"));
        if (Target.Platform == UnrealTargetPlatform.Win64)
        {
            if (!Target.bBuildEditor || ForceStaticLibInEditor)
            {
                string V8LibraryPath = Path.Combine(LibraryPath, "Win64MD");
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "wee8.lib"));
            }
            else 
            {
                WinDll(LibraryPath);
            }
        }
        else if (Target.Platform == UnrealTargetPlatform.Android)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Android", "armeabi-v7a");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libwee8.a"));
            V8LibraryPath = Path.Combine(LibraryPath, "Android", "arm64-v8a");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libwee8.a"));
        }
        else if (Target.Platform == UnrealTargetPlatform.Mac)
        {
            //PublicFrameworks.AddRange(new string[] { "WebKit",  "JavaScriptCore" });
            //PublicFrameworks.AddRange(new string[] { "WebKit" });
            if (!Target.bBuildEditor || ForceStaticLibInEditor)
            {
                LibraryPath = Path.Combine(LibraryPath, "macOS");
#if UE_5_2_OR_LATER
                if (Target.Architecture == UnrealArch.Arm64)
                {
                    LibraryPath += "_arm64";
                }
#endif
                PublicAdditionalLibraries.Add(Path.Combine(LibraryPath, "libwee8.a"));
            }
            else
            {
                LibraryPath = Path.Combine(LibraryPath, "macOSdylib");
#if UE_5_2_OR_LATER
                if (Target.Architecture == UnrealArch.Arm64)
                {
                    LibraryPath += "_arm64";
                }
#endif
                MacDylib(LibraryPath);
            }
        }
        else if (Target.Platform == UnrealTargetPlatform.IOS)
        {
            PublicFrameworks.AddRange(new string[] { "WebKit" });
            string V8LibraryPath = Path.Combine(LibraryPath, "iOS", "arm64");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libwee8.a"));
        } 
        else if (Target.Platform == UnrealTargetPlatform.Linux) 
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Linux");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libwee8.a"));
        }

        if (ForceStaticLibInEditor)
        {
            PrivateDefinitions.Add("FORCE_USE_STATIC_V8_LIB");
        }
    }
    
    void ThirdPartyNodejs(ReadOnlyTargetRules Target)
    {
        PrivateDefinitions.Add("WITH_NODEJS");
        string WsHeaderPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "Include"));
        PublicIncludePaths.AddRange(new string[] { Path.Combine(WsHeaderPath, "websocketpp") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(WsHeaderPath, "asio") });

        string NodeRoot = Node16 ? "nodejs_16" : "nodejs";

        string HeaderPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", NodeRoot));
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "include") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "deps", "v8", "include") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "deps", "uv", "include") });

        string LibraryPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", NodeRoot, "lib"));
        if (Target.Platform == UnrealTargetPlatform.Win64)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Win64");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode.lib"));

            RuntimeDependencies.Add("$(TargetOutputDir)/libnode.dll", Path.Combine(V8LibraryPath, "libnode.dll"));
            AddRuntimeDependencies(new string[] { "libnode.dll" }, V8LibraryPath, false);
        }
        else if (Target.Platform == UnrealTargetPlatform.Android)
        {
            /*
            #if UE_4_19_OR_LATER
                        AdditionalPropertiesForReceipt.Add("AndroidPlugin", Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "Libnode_APL.xml"));
            #else
                        AdditionalPropertiesForReceipt.Add(new ReceiptProperty("AndroidPlugin", Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "Libnode_APL.xml")));
            #endif
            #if UE_4_24_OR_LATER
                        PublicSystemLibraryPaths.Add(Path.Combine(LibraryPath, "Android", "armeabi-v7a"));
                        PublicSystemLibraryPaths.Add(Path.Combine(LibraryPath, "Android", "arm64-v8a"));
                        PublicSystemLibraries.Add("node");
            #else
                        PublicLibraryPaths.Add(Path.Combine(LibraryPath, "Android", "armeabi-v7a"));
                        PublicLibraryPaths.Add(Path.Combine(LibraryPath, "Android", "arm64-v8a"));
                        PublicAdditionalLibraries.Add("node");
            #endif  //UE_4_24_OR_LATER
            */
            
            string[] Archs = new string[] { "armeabi-v7a", "arm64-v8a" };
            foreach (var Arch in Archs)
            {
                string V8LibraryPath = Path.Combine(LibraryPath, "Android", Arch);
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libhistogram.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libuvwasi.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode_stub.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_snapshot.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libplatform.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libzlib.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libllhttp.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libcares.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libuv.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnghttp2.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libbrotli.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_base_without_compiler.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libbase.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_zlib.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_compiler.a"));
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_initializers.a"));
                if (!Node16)
                {
                    PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libsampler.a"));
                }
            }
            
        }
        else if (Target.Platform == UnrealTargetPlatform.Mac)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "macOS");
            if (Node16)
            {
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode.93.dylib"));
            }
            else
            {
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode.83.dylib"));
            }
        }
        else if (Target.Platform == UnrealTargetPlatform.IOS)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "iOS");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libhistogram.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libuvwasi.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode_stub.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_snapshot.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libplatform.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libzlib.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libllhttp.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libcares.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libuv.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnghttp2.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libbrotli.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libopenssl.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_base_without_compiler.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libbase.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_zlib.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_compiler.a"));
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_initializers.a"));
            if (!Node16)
            {
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libv8_libsampler.a"));
            }
        } 
        else if (Target.Platform == UnrealTargetPlatform.Linux) 
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Linux");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libnode.so"));
            RuntimeDependencies.Add("$(TargetOutputDir)/libnode.so.93", Path.Combine(V8LibraryPath, "libnode.so.93"));
        }
    }

    void ThirdPartyQJS(ReadOnlyTargetRules Target)
    {
        PrivateDefinitions.Add("WITHOUT_INSPECTOR");
        PrivateDefinitions.Add("WITH_QUICKJS");
        if (QjsNamespaceSuffix)
        {
            PublicDefinitions.Add("WITH_QJS_NAMESPACE_SUFFIX=1");
            PublicDefinitions.Add("QJSV8NAMESPACE=v8_qjs");
        }
        
        string ThirdPartyPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "ThirdParty"));
        string HeaderPath = Path.GetFullPath(Path.Combine(ThirdPartyPath, "Include"));
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "websocketpp") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(HeaderPath, "asio") });
        PublicIncludePaths.AddRange(new string[] { Path.Combine(ThirdPartyPath, "quickjs", "Inc") });

        string LibraryPath = Path.GetFullPath(Path.Combine(ThirdPartyPath, "quickjs", "Lib"));
        if (Target.Platform == UnrealTargetPlatform.Win64)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Win64MD");

            bool UsingSource = false;
            if (UsingSource)
            {
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libquickjs.dll.a"));
                PrivateDefinitions.Add("BUILDING_V8_SHARED");
            }
            else
            {
                if (Target.bBuildEditor && !ForceStaticLibInEditor)
                {
                    V8LibraryPath = Path.Combine(LibraryPath, "Win64DLL");
                    AddRuntimeDependencies(new string[] { "v8qjs.dll" }, V8LibraryPath, false);
                }

                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "quickjs.dll.lib"));
            }

            AddRuntimeDependencies(new string[] { "msys-quickjs.dll" }, V8LibraryPath, false);
            AddRuntimeDependencies(new string[]
            {
                "libgcc_s_seh-1.dll",
                "libwinpthread-1.dll"
            }, V8LibraryPath, true);
        }
        else if (Target.Platform == UnrealTargetPlatform.Android)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Android", "armeabi-v7a");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libquickjs.a"));
            V8LibraryPath = Path.Combine(LibraryPath, "Android", "arm64-v8a");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libquickjs.a"));
        }
        else if (Target.Platform == UnrealTargetPlatform.Mac)
        {
            // PublicFrameworks.AddRange(new string[] { "WebKit",  "JavaScriptCore" });
            //PublicFrameworks.AddRange(new string[] { "WebKit" });
            if (!Target.bBuildEditor || ForceStaticLibInEditor)
            {
                string V8LibraryPath = Path.Combine(LibraryPath, "macOS");
#if UE_5_2_OR_LATER
                if (Target.Architecture == UnrealArch.Arm64)
                {
                    V8LibraryPath = Path.Combine(LibraryPath, "macOS_arm64");
                }
#endif
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libquickjs.a"));
            }
            else
            {
               string V8LibraryPath = Path.Combine(LibraryPath, "macOSdylib");
               string QJSDylibName = "libquickjs.dylib";
#if UE_5_2_OR_LATER
                if (Target.Architecture == UnrealArch.Arm64)
                {
                    V8LibraryPath = Path.Combine(LibraryPath, "macOS_arm64");
                    QJSDylibName = "libquickjs.a";
                }
#endif
                PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, QJSDylibName));
            }
        }
        else if (Target.Platform == UnrealTargetPlatform.IOS)
        {
            PublicFrameworks.AddRange(new string[] { "WebKit" });
            string V8LibraryPath = Path.Combine(LibraryPath, "iOS", "arm64");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libquickjs.a"));
        }
        else if (Target.Platform == UnrealTargetPlatform.Linux)
        {
            string V8LibraryPath = Path.Combine(LibraryPath, "Linux");
            PublicAdditionalLibraries.Add(Path.Combine(V8LibraryPath, "libquickjs.a"));
        }
    }

    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
            "Source directory does not exist or could not be found: "
            + sourceDirName);
        }

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, true);
        }

        if (copySubDirs)
        {
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs);
            }
        }
    }

}
