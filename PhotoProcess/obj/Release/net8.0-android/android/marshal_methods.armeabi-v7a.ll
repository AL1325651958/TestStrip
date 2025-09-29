; ModuleID = 'marshal_methods.armeabi-v7a.ll'
source_filename = "marshal_methods.armeabi-v7a.ll"
target datalayout = "e-m:e-p:32:32-Fi8-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [119 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [238 x i32] [
	i32 42639949, ; 0: System.Threading.Thread => 0x28aa24d => 110
	i32 67008169, ; 1: zh-Hant\Microsoft.Maui.Controls.resources => 0x3fe76a9 => 33
	i32 72070932, ; 2: Microsoft.Maui.Graphics.dll => 0x44bb714 => 48
	i32 117431740, ; 3: System.Runtime.InteropServices => 0x6ffddbc => 103
	i32 165246403, ; 4: Xamarin.AndroidX.Collection.dll => 0x9d975c3 => 57
	i32 182336117, ; 5: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0xade3a75 => 75
	i32 195452805, ; 6: vi/Microsoft.Maui.Controls.resources.dll => 0xba65f85 => 30
	i32 199333315, ; 7: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xbe195c3 => 31
	i32 205061960, ; 8: System.ComponentModel => 0xc38ff48 => 87
	i32 280992041, ; 9: cs/Microsoft.Maui.Controls.resources.dll => 0x10bf9929 => 2
	i32 317674968, ; 10: vi\Microsoft.Maui.Controls.resources => 0x12ef55d8 => 30
	i32 318968648, ; 11: Xamarin.AndroidX.Activity.dll => 0x13031348 => 53
	i32 336156722, ; 12: ja/Microsoft.Maui.Controls.resources.dll => 0x14095832 => 15
	i32 342366114, ; 13: Xamarin.AndroidX.Lifecycle.Common => 0x146817a2 => 64
	i32 356389973, ; 14: it/Microsoft.Maui.Controls.resources.dll => 0x153e1455 => 14
	i32 379916513, ; 15: System.Threading.Thread.dll => 0x16a510e1 => 110
	i32 385762202, ; 16: System.Memory.dll => 0x16fe439a => 94
	i32 395744057, ; 17: _Microsoft.Android.Resource.Designer => 0x17969339 => 34
	i32 435591531, ; 18: sv/Microsoft.Maui.Controls.resources.dll => 0x19f6996b => 26
	i32 442565967, ; 19: System.Collections => 0x1a61054f => 84
	i32 450948140, ; 20: Xamarin.AndroidX.Fragment.dll => 0x1ae0ec2c => 63
	i32 465846621, ; 21: mscorlib => 0x1bc4415d => 114
	i32 469710990, ; 22: System.dll => 0x1bff388e => 113
	i32 498788369, ; 23: System.ObjectModel => 0x1dbae811 => 100
	i32 500358224, ; 24: id/Microsoft.Maui.Controls.resources.dll => 0x1dd2dc50 => 13
	i32 503918385, ; 25: fi/Microsoft.Maui.Controls.resources.dll => 0x1e092f31 => 7
	i32 513247710, ; 26: Microsoft.Extensions.Primitives.dll => 0x1e9789de => 43
	i32 521463852, ; 27: Ionic.Zlib => 0x1f14e82c => 35
	i32 525008092, ; 28: SkiaSharp.dll => 0x1f4afcdc => 49
	i32 539058512, ; 29: Microsoft.Extensions.Logging => 0x20216150 => 40
	i32 592146354, ; 30: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x234b6fb2 => 21
	i32 627609679, ; 31: Xamarin.AndroidX.CustomView => 0x2568904f => 61
	i32 627931235, ; 32: nl\Microsoft.Maui.Controls.resources => 0x256d7863 => 19
	i32 662205335, ; 33: System.Text.Encodings.Web.dll => 0x27787397 => 107
	i32 672442732, ; 34: System.Collections.Concurrent => 0x2814a96c => 82
	i32 688181140, ; 35: ca/Microsoft.Maui.Controls.resources.dll => 0x2904cf94 => 1
	i32 706645707, ; 36: ko/Microsoft.Maui.Controls.resources.dll => 0x2a1e8ecb => 16
	i32 709557578, ; 37: de/Microsoft.Maui.Controls.resources.dll => 0x2a4afd4a => 4
	i32 722857257, ; 38: System.Runtime.Loader.dll => 0x2b15ed29 => 104
	i32 759454413, ; 39: System.Net.Requests => 0x2d445acd => 97
	i32 775507847, ; 40: System.IO.Compression => 0x2e394f87 => 91
	i32 777317022, ; 41: sk\Microsoft.Maui.Controls.resources => 0x2e54ea9e => 25
	i32 789151979, ; 42: Microsoft.Extensions.Options => 0x2f0980eb => 42
	i32 823281589, ; 43: System.Private.Uri.dll => 0x311247b5 => 101
	i32 830298997, ; 44: System.IO.Compression.Brotli => 0x317d5b75 => 90
	i32 904024072, ; 45: System.ComponentModel.Primitives.dll => 0x35e25008 => 85
	i32 926902833, ; 46: tr/Microsoft.Maui.Controls.resources.dll => 0x373f6a31 => 28
	i32 958884889, ; 47: PhotoProcess => 0x39276c19 => 81
	i32 967690846, ; 48: Xamarin.AndroidX.Lifecycle.Common.dll => 0x39adca5e => 64
	i32 992768348, ; 49: System.Collections.dll => 0x3b2c715c => 84
	i32 1012816738, ; 50: Xamarin.AndroidX.SavedState.dll => 0x3c5e5b62 => 74
	i32 1028951442, ; 51: Microsoft.Extensions.DependencyInjection.Abstractions => 0x3d548d92 => 39
	i32 1029334545, ; 52: da/Microsoft.Maui.Controls.resources.dll => 0x3d5a6611 => 3
	i32 1035644815, ; 53: Xamarin.AndroidX.AppCompat => 0x3dbaaf8f => 54
	i32 1044663988, ; 54: System.Linq.Expressions.dll => 0x3e444eb4 => 92
	i32 1052210849, ; 55: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x3eb776a1 => 66
	i32 1082857460, ; 56: System.ComponentModel.TypeConverter => 0x408b17f4 => 86
	i32 1084122840, ; 57: Xamarin.Kotlin.StdLib => 0x409e66d8 => 79
	i32 1098259244, ; 58: System => 0x41761b2c => 113
	i32 1118262833, ; 59: ko\Microsoft.Maui.Controls.resources => 0x42a75631 => 16
	i32 1168523401, ; 60: pt\Microsoft.Maui.Controls.resources => 0x45a64089 => 22
	i32 1178241025, ; 61: Xamarin.AndroidX.Navigation.Runtime.dll => 0x463a8801 => 71
	i32 1203215381, ; 62: pl/Microsoft.Maui.Controls.resources.dll => 0x47b79c15 => 20
	i32 1234928153, ; 63: nb/Microsoft.Maui.Controls.resources.dll => 0x499b8219 => 18
	i32 1260983243, ; 64: cs\Microsoft.Maui.Controls.resources => 0x4b2913cb => 2
	i32 1268545293, ; 65: Ionic.Zlib.dll => 0x4b9c770d => 35
	i32 1293217323, ; 66: Xamarin.AndroidX.DrawerLayout.dll => 0x4d14ee2b => 62
	i32 1324164729, ; 67: System.Linq => 0x4eed2679 => 93
	i32 1373134921, ; 68: zh-Hans\Microsoft.Maui.Controls.resources => 0x51d86049 => 32
	i32 1376866003, ; 69: Xamarin.AndroidX.SavedState => 0x52114ed3 => 74
	i32 1406073936, ; 70: Xamarin.AndroidX.CoordinatorLayout => 0x53cefc50 => 58
	i32 1430672901, ; 71: ar\Microsoft.Maui.Controls.resources => 0x55465605 => 0
	i32 1461004990, ; 72: es\Microsoft.Maui.Controls.resources => 0x57152abe => 6
	i32 1462112819, ; 73: System.IO.Compression.dll => 0x57261233 => 91
	i32 1469204771, ; 74: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x57924923 => 55
	i32 1470490898, ; 75: Microsoft.Extensions.Primitives => 0x57a5e912 => 43
	i32 1480492111, ; 76: System.IO.Compression.Brotli.dll => 0x583e844f => 90
	i32 1493001747, ; 77: hi/Microsoft.Maui.Controls.resources.dll => 0x58fd6613 => 10
	i32 1514721132, ; 78: el/Microsoft.Maui.Controls.resources.dll => 0x5a48cf6c => 5
	i32 1543031311, ; 79: System.Text.RegularExpressions.dll => 0x5bf8ca0f => 109
	i32 1551623176, ; 80: sk/Microsoft.Maui.Controls.resources.dll => 0x5c7be408 => 25
	i32 1622152042, ; 81: Xamarin.AndroidX.Loader.dll => 0x60b0136a => 68
	i32 1623212457, ; 82: SkiaSharp.Views.Maui.Controls => 0x60c041a9 => 51
	i32 1624863272, ; 83: Xamarin.AndroidX.ViewPager2 => 0x60d97228 => 77
	i32 1636350590, ; 84: Xamarin.AndroidX.CursorAdapter => 0x6188ba7e => 60
	i32 1639515021, ; 85: System.Net.Http.dll => 0x61b9038d => 95
	i32 1639986890, ; 86: System.Text.RegularExpressions => 0x61c036ca => 109
	i32 1657153582, ; 87: System.Runtime => 0x62c6282e => 105
	i32 1658251792, ; 88: Xamarin.Google.Android.Material.dll => 0x62d6ea10 => 78
	i32 1677501392, ; 89: System.Net.Primitives.dll => 0x63fca3d0 => 96
	i32 1679769178, ; 90: System.Security.Cryptography => 0x641f3e5a => 106
	i32 1729485958, ; 91: Xamarin.AndroidX.CardView.dll => 0x6715dc86 => 56
	i32 1736233607, ; 92: ro/Microsoft.Maui.Controls.resources.dll => 0x677cd287 => 23
	i32 1743415430, ; 93: ca\Microsoft.Maui.Controls.resources => 0x67ea6886 => 1
	i32 1766324549, ; 94: Xamarin.AndroidX.SwipeRefreshLayout => 0x6947f945 => 75
	i32 1770582343, ; 95: Microsoft.Extensions.Logging.dll => 0x6988f147 => 40
	i32 1780572499, ; 96: Mono.Android.Runtime.dll => 0x6a216153 => 117
	i32 1782862114, ; 97: ms\Microsoft.Maui.Controls.resources => 0x6a445122 => 17
	i32 1788241197, ; 98: Xamarin.AndroidX.Fragment => 0x6a96652d => 63
	i32 1793755602, ; 99: he\Microsoft.Maui.Controls.resources => 0x6aea89d2 => 9
	i32 1808609942, ; 100: Xamarin.AndroidX.Loader => 0x6bcd3296 => 68
	i32 1813058853, ; 101: Xamarin.Kotlin.StdLib.dll => 0x6c111525 => 79
	i32 1813201214, ; 102: Xamarin.Google.Android.Material => 0x6c13413e => 78
	i32 1818569960, ; 103: Xamarin.AndroidX.Navigation.UI.dll => 0x6c652ce8 => 72
	i32 1828688058, ; 104: Microsoft.Extensions.Logging.Abstractions.dll => 0x6cff90ba => 41
	i32 1842015223, ; 105: uk/Microsoft.Maui.Controls.resources.dll => 0x6dcaebf7 => 29
	i32 1853025655, ; 106: sv\Microsoft.Maui.Controls.resources => 0x6e72ed77 => 26
	i32 1858542181, ; 107: System.Linq.Expressions => 0x6ec71a65 => 92
	i32 1875935024, ; 108: fr\Microsoft.Maui.Controls.resources => 0x6fd07f30 => 8
	i32 1910275211, ; 109: System.Collections.NonGeneric.dll => 0x71dc7c8b => 83
	i32 1968388702, ; 110: Microsoft.Extensions.Configuration.dll => 0x75533a5e => 36
	i32 2003115576, ; 111: el\Microsoft.Maui.Controls.resources => 0x77651e38 => 5
	i32 2019465201, ; 112: Xamarin.AndroidX.Lifecycle.ViewModel => 0x785e97f1 => 66
	i32 2025202353, ; 113: ar/Microsoft.Maui.Controls.resources.dll => 0x78b622b1 => 0
	i32 2045470958, ; 114: System.Private.Xml => 0x79eb68ee => 102
	i32 2055257422, ; 115: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x7a80bd4e => 65
	i32 2066184531, ; 116: de\Microsoft.Maui.Controls.resources => 0x7b277953 => 4
	i32 2079903147, ; 117: System.Runtime.dll => 0x7bf8cdab => 105
	i32 2090596640, ; 118: System.Numerics.Vectors => 0x7c9bf920 => 99
	i32 2127167465, ; 119: System.Console => 0x7ec9ffe9 => 88
	i32 2159891885, ; 120: Microsoft.Maui => 0x80bd55ad => 46
	i32 2169148018, ; 121: hu\Microsoft.Maui.Controls.resources => 0x814a9272 => 12
	i32 2181898931, ; 122: Microsoft.Extensions.Options.dll => 0x820d22b3 => 42
	i32 2192057212, ; 123: Microsoft.Extensions.Logging.Abstractions => 0x82a8237c => 41
	i32 2193016926, ; 124: System.ObjectModel.dll => 0x82b6c85e => 100
	i32 2201107256, ; 125: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x83323b38 => 80
	i32 2201231467, ; 126: System.Net.Http => 0x8334206b => 95
	i32 2207618523, ; 127: it\Microsoft.Maui.Controls.resources => 0x839595db => 14
	i32 2266799131, ; 128: Microsoft.Extensions.Configuration.Abstractions => 0x871c9c1b => 37
	i32 2270573516, ; 129: fr/Microsoft.Maui.Controls.resources.dll => 0x875633cc => 8
	i32 2279755925, ; 130: Xamarin.AndroidX.RecyclerView.dll => 0x87e25095 => 73
	i32 2295906218, ; 131: System.Net.Sockets => 0x88d8bfaa => 98
	i32 2303942373, ; 132: nb\Microsoft.Maui.Controls.resources => 0x89535ee5 => 18
	i32 2305521784, ; 133: System.Private.CoreLib.dll => 0x896b7878 => 115
	i32 2353062107, ; 134: System.Net.Primitives => 0x8c40e0db => 96
	i32 2364201794, ; 135: SkiaSharp.Views.Maui.Core => 0x8ceadb42 => 52
	i32 2368005991, ; 136: System.Xml.ReaderWriter.dll => 0x8d24e767 => 112
	i32 2371007202, ; 137: Microsoft.Extensions.Configuration => 0x8d52b2e2 => 36
	i32 2395872292, ; 138: id\Microsoft.Maui.Controls.resources => 0x8ece1c24 => 13
	i32 2427813419, ; 139: hi\Microsoft.Maui.Controls.resources => 0x90b57e2b => 10
	i32 2435356389, ; 140: System.Console.dll => 0x912896e5 => 88
	i32 2458678730, ; 141: System.Net.Sockets.dll => 0x928c75ca => 98
	i32 2475788418, ; 142: Java.Interop.dll => 0x93918882 => 116
	i32 2480646305, ; 143: Microsoft.Maui.Controls => 0x93dba8a1 => 44
	i32 2550873716, ; 144: hr\Microsoft.Maui.Controls.resources => 0x980b3e74 => 11
	i32 2570120770, ; 145: System.Text.Encodings.Web => 0x9930ee42 => 107
	i32 2593496499, ; 146: pl\Microsoft.Maui.Controls.resources => 0x9a959db3 => 20
	i32 2605712449, ; 147: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x9b500441 => 80
	i32 2617129537, ; 148: System.Private.Xml.dll => 0x9bfe3a41 => 102
	i32 2620871830, ; 149: Xamarin.AndroidX.CursorAdapter.dll => 0x9c375496 => 60
	i32 2625339995, ; 150: SkiaSharp.Views.Maui.Core.dll => 0x9c7b825b => 52
	i32 2626831493, ; 151: ja\Microsoft.Maui.Controls.resources => 0x9c924485 => 15
	i32 2663698177, ; 152: System.Runtime.Loader => 0x9ec4cf01 => 104
	i32 2732626843, ; 153: Xamarin.AndroidX.Activity => 0xa2e0939b => 53
	i32 2737747696, ; 154: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0xa32eb6f0 => 55
	i32 2752995522, ; 155: pt-BR\Microsoft.Maui.Controls.resources => 0xa41760c2 => 21
	i32 2758225723, ; 156: Microsoft.Maui.Controls.Xaml => 0xa4672f3b => 45
	i32 2764765095, ; 157: Microsoft.Maui.dll => 0xa4caf7a7 => 46
	i32 2778768386, ; 158: Xamarin.AndroidX.ViewPager.dll => 0xa5a0a402 => 76
	i32 2785988530, ; 159: th\Microsoft.Maui.Controls.resources => 0xa60ecfb2 => 27
	i32 2795602088, ; 160: SkiaSharp.Views.Android.dll => 0xa6a180a8 => 50
	i32 2801831435, ; 161: Microsoft.Maui.Graphics => 0xa7008e0b => 48
	i32 2806116107, ; 162: es/Microsoft.Maui.Controls.resources.dll => 0xa741ef0b => 6
	i32 2810250172, ; 163: Xamarin.AndroidX.CoordinatorLayout.dll => 0xa78103bc => 58
	i32 2831556043, ; 164: nl/Microsoft.Maui.Controls.resources.dll => 0xa8c61dcb => 19
	i32 2853208004, ; 165: Xamarin.AndroidX.ViewPager => 0xaa107fc4 => 76
	i32 2861189240, ; 166: Microsoft.Maui.Essentials => 0xaa8a4878 => 47
	i32 2905242038, ; 167: mscorlib.dll => 0xad2a79b6 => 114
	i32 2909740682, ; 168: System.Private.CoreLib => 0xad6f1e8a => 115
	i32 2912489636, ; 169: SkiaSharp.Views.Android => 0xad9910a4 => 50
	i32 2916838712, ; 170: Xamarin.AndroidX.ViewPager2.dll => 0xaddb6d38 => 77
	i32 2919462931, ; 171: System.Numerics.Vectors.dll => 0xae037813 => 99
	i32 2959614098, ; 172: System.ComponentModel.dll => 0xb0682092 => 87
	i32 2978675010, ; 173: Xamarin.AndroidX.DrawerLayout => 0xb18af942 => 62
	i32 3038032645, ; 174: _Microsoft.Android.Resource.Designer.dll => 0xb514b305 => 34
	i32 3057625584, ; 175: Xamarin.AndroidX.Navigation.Common => 0xb63fa9f0 => 69
	i32 3059408633, ; 176: Mono.Android.Runtime => 0xb65adef9 => 117
	i32 3059793426, ; 177: System.ComponentModel.Primitives => 0xb660be12 => 85
	i32 3077302341, ; 178: hu/Microsoft.Maui.Controls.resources.dll => 0xb76be845 => 12
	i32 3178803400, ; 179: Xamarin.AndroidX.Navigation.Fragment.dll => 0xbd78b0c8 => 70
	i32 3220365878, ; 180: System.Threading => 0xbff2e236 => 111
	i32 3258312781, ; 181: Xamarin.AndroidX.CardView => 0xc235e84d => 56
	i32 3305363605, ; 182: fi\Microsoft.Maui.Controls.resources => 0xc503d895 => 7
	i32 3316684772, ; 183: System.Net.Requests.dll => 0xc5b097e4 => 97
	i32 3317135071, ; 184: Xamarin.AndroidX.CustomView.dll => 0xc5b776df => 61
	i32 3340387945, ; 185: SkiaSharp => 0xc71a4669 => 49
	i32 3346324047, ; 186: Xamarin.AndroidX.Navigation.Runtime => 0xc774da4f => 71
	i32 3357674450, ; 187: ru\Microsoft.Maui.Controls.resources => 0xc8220bd2 => 24
	i32 3358260929, ; 188: System.Text.Json => 0xc82afec1 => 108
	i32 3362522851, ; 189: Xamarin.AndroidX.Core => 0xc86c06e3 => 59
	i32 3366347497, ; 190: Java.Interop => 0xc8a662e9 => 116
	i32 3374999561, ; 191: Xamarin.AndroidX.RecyclerView => 0xc92a6809 => 73
	i32 3381016424, ; 192: da\Microsoft.Maui.Controls.resources => 0xc9863768 => 3
	i32 3425412350, ; 193: PhotoProcess.dll => 0xcc2ba4fe => 81
	i32 3428513518, ; 194: Microsoft.Extensions.DependencyInjection.dll => 0xcc5af6ee => 38
	i32 3463511458, ; 195: hr/Microsoft.Maui.Controls.resources.dll => 0xce70fda2 => 11
	i32 3471940407, ; 196: System.ComponentModel.TypeConverter.dll => 0xcef19b37 => 86
	i32 3473156932, ; 197: SkiaSharp.Views.Maui.Controls.dll => 0xcf042b44 => 51
	i32 3476120550, ; 198: Mono.Android => 0xcf3163e6 => 118
	i32 3479583265, ; 199: ru/Microsoft.Maui.Controls.resources.dll => 0xcf663a21 => 24
	i32 3484440000, ; 200: ro\Microsoft.Maui.Controls.resources => 0xcfb055c0 => 23
	i32 3485117614, ; 201: System.Text.Json.dll => 0xcfbaacae => 108
	i32 3580758918, ; 202: zh-HK\Microsoft.Maui.Controls.resources => 0xd56e0b86 => 31
	i32 3608519521, ; 203: System.Linq.dll => 0xd715a361 => 93
	i32 3641597786, ; 204: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0xd90e5f5a => 65
	i32 3643446276, ; 205: tr\Microsoft.Maui.Controls.resources => 0xd92a9404 => 28
	i32 3643854240, ; 206: Xamarin.AndroidX.Navigation.Fragment => 0xd930cda0 => 70
	i32 3657292374, ; 207: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd9fdda56 => 37
	i32 3672681054, ; 208: Mono.Android.dll => 0xdae8aa5e => 118
	i32 3697841164, ; 209: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xdc68940c => 33
	i32 3724971120, ; 210: Xamarin.AndroidX.Navigation.Common.dll => 0xde068c70 => 69
	i32 3748608112, ; 211: System.Diagnostics.DiagnosticSource => 0xdf6f3870 => 89
	i32 3786282454, ; 212: Xamarin.AndroidX.Collection => 0xe1ae15d6 => 57
	i32 3792276235, ; 213: System.Collections.NonGeneric => 0xe2098b0b => 83
	i32 3823082795, ; 214: System.Security.Cryptography.dll => 0xe3df9d2b => 106
	i32 3841636137, ; 215: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xe4fab729 => 39
	i32 3849253459, ; 216: System.Runtime.InteropServices.dll => 0xe56ef253 => 103
	i32 3889960447, ; 217: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xe7dc15ff => 32
	i32 3896106733, ; 218: System.Collections.Concurrent.dll => 0xe839deed => 82
	i32 3896760992, ; 219: Xamarin.AndroidX.Core.dll => 0xe843daa0 => 59
	i32 3928044579, ; 220: System.Xml.ReaderWriter => 0xea213423 => 112
	i32 3931092270, ; 221: Xamarin.AndroidX.Navigation.UI => 0xea4fb52e => 72
	i32 3955647286, ; 222: Xamarin.AndroidX.AppCompat.dll => 0xebc66336 => 54
	i32 3980434154, ; 223: th/Microsoft.Maui.Controls.resources.dll => 0xed409aea => 27
	i32 3987592930, ; 224: he/Microsoft.Maui.Controls.resources.dll => 0xedadd6e2 => 9
	i32 4025784931, ; 225: System.Memory => 0xeff49a63 => 94
	i32 4046471985, ; 226: Microsoft.Maui.Controls.Xaml.dll => 0xf1304331 => 45
	i32 4073602200, ; 227: System.Threading.dll => 0xf2ce3c98 => 111
	i32 4094352644, ; 228: Microsoft.Maui.Essentials.dll => 0xf40add04 => 47
	i32 4100113165, ; 229: System.Private.Uri => 0xf462c30d => 101
	i32 4102112229, ; 230: pt/Microsoft.Maui.Controls.resources.dll => 0xf48143e5 => 22
	i32 4125707920, ; 231: ms/Microsoft.Maui.Controls.resources.dll => 0xf5e94e90 => 17
	i32 4126470640, ; 232: Microsoft.Extensions.DependencyInjection => 0xf5f4f1f0 => 38
	i32 4150914736, ; 233: uk\Microsoft.Maui.Controls.resources => 0xf769eeb0 => 29
	i32 4182413190, ; 234: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0xf94a8f86 => 67
	i32 4213026141, ; 235: System.Diagnostics.DiagnosticSource.dll => 0xfb1dad5d => 89
	i32 4271975918, ; 236: Microsoft.Maui.Controls.dll => 0xfea12dee => 44
	i32 4292120959 ; 237: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xffd4917f => 67
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [238 x i32] [
	i32 110, ; 0
	i32 33, ; 1
	i32 48, ; 2
	i32 103, ; 3
	i32 57, ; 4
	i32 75, ; 5
	i32 30, ; 6
	i32 31, ; 7
	i32 87, ; 8
	i32 2, ; 9
	i32 30, ; 10
	i32 53, ; 11
	i32 15, ; 12
	i32 64, ; 13
	i32 14, ; 14
	i32 110, ; 15
	i32 94, ; 16
	i32 34, ; 17
	i32 26, ; 18
	i32 84, ; 19
	i32 63, ; 20
	i32 114, ; 21
	i32 113, ; 22
	i32 100, ; 23
	i32 13, ; 24
	i32 7, ; 25
	i32 43, ; 26
	i32 35, ; 27
	i32 49, ; 28
	i32 40, ; 29
	i32 21, ; 30
	i32 61, ; 31
	i32 19, ; 32
	i32 107, ; 33
	i32 82, ; 34
	i32 1, ; 35
	i32 16, ; 36
	i32 4, ; 37
	i32 104, ; 38
	i32 97, ; 39
	i32 91, ; 40
	i32 25, ; 41
	i32 42, ; 42
	i32 101, ; 43
	i32 90, ; 44
	i32 85, ; 45
	i32 28, ; 46
	i32 81, ; 47
	i32 64, ; 48
	i32 84, ; 49
	i32 74, ; 50
	i32 39, ; 51
	i32 3, ; 52
	i32 54, ; 53
	i32 92, ; 54
	i32 66, ; 55
	i32 86, ; 56
	i32 79, ; 57
	i32 113, ; 58
	i32 16, ; 59
	i32 22, ; 60
	i32 71, ; 61
	i32 20, ; 62
	i32 18, ; 63
	i32 2, ; 64
	i32 35, ; 65
	i32 62, ; 66
	i32 93, ; 67
	i32 32, ; 68
	i32 74, ; 69
	i32 58, ; 70
	i32 0, ; 71
	i32 6, ; 72
	i32 91, ; 73
	i32 55, ; 74
	i32 43, ; 75
	i32 90, ; 76
	i32 10, ; 77
	i32 5, ; 78
	i32 109, ; 79
	i32 25, ; 80
	i32 68, ; 81
	i32 51, ; 82
	i32 77, ; 83
	i32 60, ; 84
	i32 95, ; 85
	i32 109, ; 86
	i32 105, ; 87
	i32 78, ; 88
	i32 96, ; 89
	i32 106, ; 90
	i32 56, ; 91
	i32 23, ; 92
	i32 1, ; 93
	i32 75, ; 94
	i32 40, ; 95
	i32 117, ; 96
	i32 17, ; 97
	i32 63, ; 98
	i32 9, ; 99
	i32 68, ; 100
	i32 79, ; 101
	i32 78, ; 102
	i32 72, ; 103
	i32 41, ; 104
	i32 29, ; 105
	i32 26, ; 106
	i32 92, ; 107
	i32 8, ; 108
	i32 83, ; 109
	i32 36, ; 110
	i32 5, ; 111
	i32 66, ; 112
	i32 0, ; 113
	i32 102, ; 114
	i32 65, ; 115
	i32 4, ; 116
	i32 105, ; 117
	i32 99, ; 118
	i32 88, ; 119
	i32 46, ; 120
	i32 12, ; 121
	i32 42, ; 122
	i32 41, ; 123
	i32 100, ; 124
	i32 80, ; 125
	i32 95, ; 126
	i32 14, ; 127
	i32 37, ; 128
	i32 8, ; 129
	i32 73, ; 130
	i32 98, ; 131
	i32 18, ; 132
	i32 115, ; 133
	i32 96, ; 134
	i32 52, ; 135
	i32 112, ; 136
	i32 36, ; 137
	i32 13, ; 138
	i32 10, ; 139
	i32 88, ; 140
	i32 98, ; 141
	i32 116, ; 142
	i32 44, ; 143
	i32 11, ; 144
	i32 107, ; 145
	i32 20, ; 146
	i32 80, ; 147
	i32 102, ; 148
	i32 60, ; 149
	i32 52, ; 150
	i32 15, ; 151
	i32 104, ; 152
	i32 53, ; 153
	i32 55, ; 154
	i32 21, ; 155
	i32 45, ; 156
	i32 46, ; 157
	i32 76, ; 158
	i32 27, ; 159
	i32 50, ; 160
	i32 48, ; 161
	i32 6, ; 162
	i32 58, ; 163
	i32 19, ; 164
	i32 76, ; 165
	i32 47, ; 166
	i32 114, ; 167
	i32 115, ; 168
	i32 50, ; 169
	i32 77, ; 170
	i32 99, ; 171
	i32 87, ; 172
	i32 62, ; 173
	i32 34, ; 174
	i32 69, ; 175
	i32 117, ; 176
	i32 85, ; 177
	i32 12, ; 178
	i32 70, ; 179
	i32 111, ; 180
	i32 56, ; 181
	i32 7, ; 182
	i32 97, ; 183
	i32 61, ; 184
	i32 49, ; 185
	i32 71, ; 186
	i32 24, ; 187
	i32 108, ; 188
	i32 59, ; 189
	i32 116, ; 190
	i32 73, ; 191
	i32 3, ; 192
	i32 81, ; 193
	i32 38, ; 194
	i32 11, ; 195
	i32 86, ; 196
	i32 51, ; 197
	i32 118, ; 198
	i32 24, ; 199
	i32 23, ; 200
	i32 108, ; 201
	i32 31, ; 202
	i32 93, ; 203
	i32 65, ; 204
	i32 28, ; 205
	i32 70, ; 206
	i32 37, ; 207
	i32 118, ; 208
	i32 33, ; 209
	i32 69, ; 210
	i32 89, ; 211
	i32 57, ; 212
	i32 83, ; 213
	i32 106, ; 214
	i32 39, ; 215
	i32 103, ; 216
	i32 32, ; 217
	i32 82, ; 218
	i32 59, ; 219
	i32 112, ; 220
	i32 72, ; 221
	i32 54, ; 222
	i32 27, ; 223
	i32 9, ; 224
	i32 94, ; 225
	i32 45, ; 226
	i32 111, ; 227
	i32 47, ; 228
	i32 101, ; 229
	i32 22, ; 230
	i32 17, ; 231
	i32 38, ; 232
	i32 29, ; 233
	i32 67, ; 234
	i32 89, ; 235
	i32 44, ; 236
	i32 67 ; 237
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 4

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 4

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 4

; Functions

; Function attributes: "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.str.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 4, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+armv7-a,+d32,+dsp,+fp64,+neon,+vfp2,+vfp2sp,+vfp3,+vfp3d16,+vfp3d16sp,+vfp3sp,-aes,-fp-armv8,-fp-armv8d16,-fp-armv8d16sp,-fp-armv8sp,-fp16,-fp16fml,-fullfp16,-sha2,-thumb-mode,-vfp4,-vfp4d16,-vfp4d16sp,-vfp4sp" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+armv7-a,+d32,+dsp,+fp64,+neon,+vfp2,+vfp2sp,+vfp3,+vfp3d16,+vfp3d16sp,+vfp3sp,-aes,-fp-armv8,-fp-armv8d16,-fp-armv8d16sp,-fp-armv8sp,-fp16,-fp16fml,-fullfp16,-sha2,-thumb-mode,-vfp4,-vfp4d16,-vfp4d16sp,-vfp4sp" }

; Metadata
!llvm.module.flags = !{!0, !1, !7}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"min_enum_size", i32 4}
