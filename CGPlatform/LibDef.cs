//#define LINUX
//#define WINDOWS
//#define MACOS

#if (!WINDOWS)

    #define POSIX

#endif

using System;
using System.Runtime.InteropServices;


namespace SharpGL
{
    #if ((LINUX && WINDOWS) || (LINUX && MACOS) || (WINDOWS && MACOS))
        
        #error Должено присутсовать только одно определение LINUX, WINDOWS или MACOS
    
    #elif (!(LINUX || WINDOWS || MACOS))
    
        #error Отсутсвие определения для целевой операционной системы
    
    #endif
    
    
    
    
    public partial class OpenGL
    {
        #if (LINUX)

            private const string lib_OGL = "libGL.so";
            private const string lib_GLU = "libGLU.so"; // "libGLU.so.1"
        
        #elif (WINDOWS)
        
            private const string lib_OGL = "opengl32.dll";
            private const string lib_GLU = "glu32.dll";
        
        #elif (MACOS)
        
            // NOTE: Сам не проверял, но по идее должно быть так...
            private const string lib_OGL = "GL";
            private const string lib_GLU = "GLU";
        
        #else
        
            // 
            private const string lib_OGL = "$*?|";
            private const string lib_GLU = "$*?|";
        
        #endif
    }
    
    public static class DynLoad
    {
        [Flags]
        public enum RTLD : int
        {
            /// <summary>
            /// Lazy function call binding.
            /// </summary>
            LAZY = 0x00001,
                
            /// <summary>
            /// Immediate function call binding.
            /// </summary>
            NOW	 = 0x00002,
                
            /// <summary>
            /// Mask of binding time value.
            /// </summary>
            BINDING_MASK =  0x3,
                
                
            /// <summary>
            /// Do not load the object.
            /// </summary>
            NOLOAD = 0x00004,
                
            /// <summary>
            /// Use deep binding.
            /// </summary>
            DEEPBIND = 0x00008,

            /// <summary>
            /// If the following bit is set in the MODE argument to `dlopen',
            /// the symbols of the loaded object and its dependencies are made
            /// visible as if the object were linked directly into the program.
            /// </summary>
            GLOBAL	= 0x00100,

            /// <summary>
            /// Unix98 demands the following flag which is the inverse to RTLD_GLOBAL.
            /// The implementation does this by default and so we can define the
            /// value to zero.
            /// </summary>
            LOCAL = 0,

            /// <summary>
            /// Do not delete object when closed.
            /// </summary>
            NODELETE = 0x01000
        }
        
        #if POSIX
        
            public const string lib_DL = "dl";
            
            [DllImport(lib_DL, SetLastError = false)]
            public static extern IntPtr dlopen(string filename, RTLD flags);
        
        #else
        
            public const string Kernel32 = "kernel32.dll";
            
            [DllImport(Kernel32, SetLastError = true)]
            public static extern IntPtr LoadLibrary(string lpFileName);
        
            public static IntPtr dlopen(string filename, RTLD flags)
            {
                return LoadLibrary(filename);
            }
        
        #endif
        
    }
    
    
}