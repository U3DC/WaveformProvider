﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es.InkPainter;
using UnityEngine;

namespace Es.WaveformProvider
{
	/// <summary>
	/// generates a waveform from input and outputs it as a texture.
	/// </summary>
	[RequireComponent (typeof (Renderer))]
	[DisallowMultipleComponent]
	public class WaveConductor : MonoBehaviour
	{
		#region Serialized field.

		[Range (1, 10)]
		public int updateFrameTiming = 3;

		[Range (0f, 1f)]
		public float adjuster = 0f;

		[Range (0.01f, 2f)]
		public float stride = 1f;

		[Range (0.1f, 0.98f)]
		public float attenuation = 0.96f;

		[Range (0.01f, 0.5f)]
		public float propagationSpeed = 0.1f;

		[SerializeField]
		private int inputTextureSize = 512;

		[SerializeField]
		private RenderTexture output;

		#endregion

		public bool debug;

		private static Material waveMaterial;
		private Texture2D init;
		private RenderTexture input;
		private RenderTexture prev;
		private RenderTexture prev2;
		private RenderTexture result;

		private readonly int ShaderPropertyAdjust = Shader.PropertyToID ("_RoundAdjuster");
		private readonly int ShaderPropertyStride = Shader.PropertyToID ("_Stride");
		private readonly int ShaderPropertyAttenuation = Shader.PropertyToID ("_Attenuation");
		private readonly int ShaderPropertyC = Shader.PropertyToID ("_C");
		private readonly int ShaderPropertyInputTex = Shader.PropertyToID ("_InputTex");
		private readonly int ShaderPropertyPrevTex = Shader.PropertyToID ("_PrevTex");
		private readonly int ShaderPropertyPrev2Tex = Shader.PropertyToID ("_Prev2Tex");

		/// <summary>
		/// Waveform data output texture.
		/// </summary>
		/// <returns>RenderTexture that stores the height of the wave.</returns>
		public RenderTexture Output
		{
			get { return output; }
			set { output = value; }
		}

		private Material WaveMaterial
		{
			get
			{
				if (waveMaterial == null)
					waveMaterial = new Material (Resources.Load<Material> ("Es.WaveformProvider.WaveProvide"));
				return waveMaterial;
			}
		}

		private void Awake ()
		{
			#region Create InkCanvas component

			var inputMaterial = new Material (Resources.Load<Material> ("Es.WaveformProvider.WaveInput"));
			InkCanvas.PaintSet paintSet = new InkCanvas.PaintSet ("", "", "_ParallaxMap", false, false, true, inputMaterial);
			var inkCanvas = gameObject.AddInkCanvas (paintSet);
			inkCanvas.hideFlags = HideFlags.HideInInspector;

			#endregion Create InkCanvas component

			#region Initialize texture

			inkCanvas.OnInitializedAfter += canvas =>
			{
				paintSet.paintHeightTexture = new RenderTexture (inputTextureSize, inputTextureSize, 0, RenderTextureFormat.R8);

				init = new Texture2D (1, 1);
				init.SetPixel (0, 0, new Color (0, 0, 0, 0));
				init.Apply ();

				input = paintSet.paintHeightTexture;
				prev = new RenderTexture (input.width, input.height, 0, RenderTextureFormat.R8);
				prev2 = new RenderTexture (input.width, input.height, 0, RenderTextureFormat.R8);
				result = new RenderTexture (input.width, input.height, 0, RenderTextureFormat.R8);

				var r8Init = new Texture2D (1, 1);
				r8Init.SetPixel (0, 0, new Color (0.5f, 0, 0, 1));
				r8Init.Apply ();
				Graphics.Blit (r8Init, prev);
				Graphics.Blit (r8Init, prev2);
			};

			#endregion Initialize texture
		}

		private void OnWillRenderObject ()
		{
			WaveUpdate ();
		}

		private void WaveUpdate ()
		{
			if (Time.frameCount % updateFrameTiming != 0)
				return;

			if (input == null || output == null)
				return;

			WaveMaterial.SetFloat (ShaderPropertyAdjust, adjuster);
			WaveMaterial.SetFloat (ShaderPropertyStride, stride);
			WaveMaterial.SetFloat (ShaderPropertyAttenuation, attenuation);
			WaveMaterial.SetFloat (ShaderPropertyC, propagationSpeed);
			WaveMaterial.SetTexture (ShaderPropertyInputTex, input);
			WaveMaterial.SetTexture (ShaderPropertyPrevTex, prev);
			WaveMaterial.SetTexture (ShaderPropertyPrev2Tex, prev2);

			Graphics.Blit (null, result, WaveMaterial);

			var tmp = prev2;
			prev2 = prev;
			prev = result;
			result = tmp;

			Graphics.Blit (init, input);
			Graphics.Blit (prev, output);
		}

		private void OnGUI ()
		{
			if (debug)
			{
				var h = Screen.height / 3;
				const int StrWidth = 20;
				GUI.Box (new Rect (0, 0, h, h * 3), "");
				GUI.DrawTexture (new Rect (0, 0 * h, h, h), input);
				GUI.DrawTexture (new Rect (0, 1 * h, h, h), prev);
				GUI.DrawTexture (new Rect (0, 2 * h, h, h), prev2);
				GUI.Box (new Rect (0, 1 * h - StrWidth, h, StrWidth), "INPUT");
				GUI.Box (new Rect (0, 2 * h - StrWidth, h, StrWidth), "PREV");
				GUI.Box (new Rect (0, 3 * h - StrWidth, h, StrWidth), "PREV2");
			}
		}
	}
}