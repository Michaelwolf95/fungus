using UnityEngine;
using System;
using System.Collections;
using Fungus;

namespace Fungus
{
	// Controller for main camera.
	// Supports several types of camera transition including snap, pan & fade.
	public class CameraController : MonoBehaviour 
	{
		// Fixed Z coordinate of camera
		public float cameraZ = - 10f;

		Camera mainCamera;

		void Start()
		{
			GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
			if (cameraObject == null)
			{
				Debug.LogError("Failed to find game object with tag 'MainCamera'");
				return;
			}
			mainCamera = cameraObject.GetComponent<Camera>();
			if (mainCamera == null)
			{
				Debug.LogError("Failed to find camera component");
				return;
			}
		}

		public Texture2D fadeTexture;

		public float fadeAlpha = 1f;

		void OnGUI()
		{	
			int drawDepth = -1000;

			if (fadeAlpha < 1f)
			{
				// 1 = scene fully visible
				// 0 = scene fully obscured
				GUI.color = new Color(1,1,1, 1f - fadeAlpha);	
				GUI.depth = drawDepth;
				GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
			}
		}

		public void Fade(float targetAlpha, float fadeDuration, Action fadeAction)
		{
			StartCoroutine(FadeInternal(targetAlpha, fadeDuration, fadeAction));
		}

		public void FadeToView(View view, float fadeDuration, Action fadeAction)
		{
			// Fade out
			Fade(0f, fadeDuration / 2f, delegate {
				
				// Snap to new view
				PanToView(view, 0f, null);

				// Fade in
				Fade(1f, fadeDuration / 2f, delegate {
					if (fadeAction != null)
					{
						fadeAction();
					}
				});
			});
		}

		IEnumerator FadeInternal(float targetAlpha, float fadeDuration, Action fadeAction)
		{
			float startAlpha = fadeAlpha;
			float timer = 0;

			while (timer < fadeDuration)
			{
				float t = timer / fadeDuration;
				timer += Time.deltaTime;

				t = Mathf.Clamp01(t);   

				fadeAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
				yield return null;
			}

			fadeAlpha = targetAlpha;

			if (fadeAction != null)
			{
				fadeAction();
			}
		}

		// Position camera so sprite is centered and fills the screen
		public void CenterOnSprite(SpriteRenderer spriteRenderer)
		{
			Sprite sprite = spriteRenderer.sprite;
			Vector3 extents = sprite.bounds.extents;
			float localScaleY = spriteRenderer.transform.localScale.y;
			mainCamera.orthographicSize = extents.y * localScaleY;
			
			Vector3 pos = spriteRenderer.transform.position;
			mainCamera.transform.position = new Vector3(pos.x, pos.y, 0);
			SetCameraZ();
		}

		public void SnapToView(View view)
		{
			PanToView(view, 0, null);
		}

		public void PanToView(View view, float duration, Action arriveAction)
		{
			if (duration == 0f)
			{
				// Move immediately
				mainCamera.orthographicSize = view.viewSize;
				mainCamera.transform.position = view.transform.position;
				SetCameraZ();
				if (arriveAction != null)
				{
					arriveAction();
				}
			}
			else
			{
				StartCoroutine(PanInternal(view, duration, arriveAction));
			}
		}

		IEnumerator PanInternal(View view, float duration, Action arriveAction)
		{
			float timer = 0;
			float startSize = mainCamera.orthographicSize;
			float endSize = view.viewSize;
			Vector3 startPos = mainCamera.transform.position;
			Vector3 endPos = view.transform.position;

			bool arrived = false;
			while (!arrived)
			{
				timer += Time.deltaTime;
				if (timer > duration)
				{
					arrived = true;
					timer = duration;
				}

				float t = timer / duration;
				mainCamera.orthographicSize = Mathf.Lerp(startSize, endSize, Mathf.SmoothStep(0f, 1f, t));
				mainCamera.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
				SetCameraZ();

				if (arrived &&
				    arriveAction != null)
				{
					arriveAction();
				}

				yield return null;
			}
		}

		void SetCameraZ()
		{
			mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, cameraZ);
		}
	}
}
