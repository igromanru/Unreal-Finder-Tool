﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using net.r_eg.Conari.Types;
using SdkLang.Langs;
using SdkLang.Utils;

namespace SdkLang
{
    /*
     * NOTEs:
     * 1-
     *      When you edit code on this project (SdkLang),
     *      You Must REBUILD not BUILD.
     *      Since DllExplore need to REBUILD every time.
     *
     * 2-
     *      Don't use string as file container
     *      (Read file or contact string to write a file),
     *      use UftStringBuilder instead,
     *      it's way faster and less resources.
     */

    public static class Main
    {
        public static Dictionary<string, UftLang> SupportedLangs = new Dictionary<string, UftLang>()
        {
            { "Cpp", new UftCpp() }
        };

        public static string IncludePath { get; private set; }
        public static UftLang Lang { get; set; }
        public static SdkGenInfo GenInfo { get; set; }

        private static SdkPackage GetPackageFromPtr(IntPtr nPackage)
        {
            var us = new UnmanagedStructure(nPackage, typeof(Native.Package));
            return new SdkPackage((Native.Package)us.Managed);
        }

        [DllExport]
        public static bool UftLangInit(IntPtr genInfo)
        {
            GenInfo = new SdkGenInfo((Native.GenInfo)new UnmanagedStructure(genInfo, typeof(Native.GenInfo)).Managed);
            IncludePath = GenInfo.LangPath + (GenInfo.IsExternal ? @"\External" : @"\Internal");

            // Check if this lang is supported
            if (!SupportedLangs.ContainsKey(GenInfo.SdkLang))
                return false;

            Lang = SupportedLangs[GenInfo.SdkLang];
            Lang.Init();

            return true;
        }
        [DllExport]
        public static void UftLangSaveStructs(IntPtr nPackage)
        {
            Lang.SaveStructs(GetPackageFromPtr(nPackage));
        }
        [DllExport]
        public static void UftLangSaveClasses(IntPtr nPackage)
        {
            Lang.SaveClasses(GetPackageFromPtr(nPackage));
        }
        [DllExport]
        public static void UftLangSaveFunctions(IntPtr nPackage)
        {
            Lang.SaveFunctions(GetPackageFromPtr(nPackage));
        }
        [DllExport]
        public static void UftLangSaveFunctionParameters(IntPtr nPackage)
        {
            Lang.SaveFunctionParameters(GetPackageFromPtr(nPackage));
        }
        [DllExport]
        public static void UftLangSdkAfterFinish(Native.StructArray packages, Native.StructArray missing)
        {
            // Init packages
            var packagesList = new CTypes.UftArrayPtr(packages.Ptr, packages.Count, packages.ItemSize)
                .ToPtrStructList<Native.Package>()
                .Select(nVar => new SdkPackage(nVar))
                .ToList();

            // Init missing
            var missingList = new CTypes.UftArrayPtr(missing.Ptr, missing.Count, missing.ItemSize)
                .ToPtrStructList<Native.UStruct>()
                .Select(nVar => new SdkUStruct(nVar))
                .ToList();

            Lang.SdkAfterFinish(packagesList, missingList);
        }
    }
}