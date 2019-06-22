Shader "Marching Cubes/Tst Shader"
{
	SubShader{
			Pass{
			GLSLPROGRAM

				#extension GL_EXT_gpu_shader4 : require
				flat varying vec4 color;

			#ifdef VERTEX
			void main()
			{
				color = gl_Color;
				gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
			}
			#endif

			#ifdef FRAGMENT
			void main()
			{
				gl_FragColor = color; // set the output fragment color
			}
			#endif

			ENDGLSL
			}
	}
}