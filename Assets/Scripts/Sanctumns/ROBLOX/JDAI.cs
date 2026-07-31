using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class sanctumnJDAI : MonoBehaviour
{
	[System.Serializable]
	public struct DistanceState
	{
		[Tooltip("The fraction of the radius threshold. (e.g., 1.3 means radius / 1.3)")]
		public float radiusDivisor;

		[Header("Speed Settings")]
		[Tooltip("The multiplier applied to the max nav speed for this zone.")]
		[Range(0f, 2f)] public float speedMultiplier;

		[Tooltip("Custom multiplier for the animator speed when inside this zone.")]
		[Range(0f, 3f)] public float animSpeedMultiplier;

		[Header("Audio Settings")]
		[Tooltip("The specific audio clip index to play in this zone.")]
		public int clipIndex;
	}

	[Header("References")]
	[SerializeField] private GameObject JD;
	[SerializeField] private GameObject player;
	[SerializeField] private NavMeshAgent nav;
	[SerializeField] private Animator anim;
	[SerializeField] private SphereCollider rad;
	[SerializeField] private GameObject InfectPartSys;
	[SerializeField] private JDPartData PartData;
	[SerializeField] private AudioSource source;
	[SerializeField] private AudioClip[] clips;

	[Header("Movement Tuning")]
	[SerializeField] private float navmaxspeed = 8f;
	[SerializeField] private float exitNavSpeed = 0.5f;

	[Header("Visual & Fog Tuning")]
	[SerializeField] private float fogLerpSpeed = 5f;
	[SerializeField] private bool changeFogColor = true;
	[SerializeField] private Gradient fogColorGradient;

	[Header("Particle Optimization & Customization")]
	[SerializeField] private bool updateParticles = true;
	[SerializeField] private float maxParticleLifetime = 5f;
	[SerializeField] private float lifetimeMultiplier = 1f;
	[SerializeField] private float particleShapeRadiusMultiplier = 2f;
	[SerializeField] private float maxParticleRadius = 15f;
	[SerializeField] private float orbitalVelocityMultiplier = 1f;
	[SerializeField] private float noiseStrengthMultiplier = 1f;
	[SerializeField] private float maxNoiseStrength = 5f;

	[Header("Particle Emission Ramping")]
	[SerializeField] private float minEmissionRate = 10f;
	[SerializeField] private float maxEmissionRate = 150f;

	[Header("Custom Distance Layers")]
	[SerializeField] private List<DistanceState> distanceZones = new List<DistanceState>()
	{
		new DistanceState { radiusDivisor = 1.3f, speedMultiplier = 0.166f, animSpeedMultiplier = 0.33f, clipIndex = 0 },
		new DistanceState { radiusDivisor = 2.3f, speedMultiplier = 0.25f,  animSpeedMultiplier = 0.5f,  clipIndex = 1 },
		new DistanceState { radiusDivisor = 3.3f, speedMultiplier = 0.5f,   animSpeedMultiplier = 1.0f,  clipIndex = 2 },
		new DistanceState { radiusDivisor = 4.3f, speedMultiplier = 1.0f,   animSpeedMultiplier = 2.0f,  clipIndex = 3 }
	};

	[Header("Debug Settings")]
	[SerializeField] private bool showDebugGizmos = true;
	[SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);

	private float fogDefDistance;
	private Color fogDefColor;
	private float currentFogEnd;
	private Color currentFogColor;
	private bool isLockedToMaxStage = false;

	private void Start()
	{
		if (RenderSettings.fog)
		{
			fogDefDistance = RenderSettings.fogEndDistance;
			fogDefColor = RenderSettings.fogColor;
			currentFogEnd = RenderSettings.fogEndDistance;
			currentFogColor = RenderSettings.fogColor;
		}
	}

	private void Update()
	{
		if (player != null && nav != null && nav.gameObject.activeInHierarchy && nav.isOnNavMesh)
		{
			nav.SetDestination(player.transform.position);
		}

		if (RenderSettings.fog)
		{
			RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, currentFogEnd, Time.deltaTime * fogLerpSpeed);

			if (changeFogColor)
			{
				RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, currentFogColor, Time.deltaTime * fogLerpSpeed);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject != player) return;

		if (JD != null) JD.SetActive(true);
		if (InfectPartSys != null) InfectPartSys.SetActive(true);

		if (player.TryGetComponent<AudioSource>(out AudioSource playerAudio))
		{
			playerAudio.Stop();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject != player || rad == null) return;

		float dist = Vector3.Distance(other.transform.position, transform.position);
		if (InfectPartSys != null) InfectPartSys.transform.position = other.transform.position;

		currentFogEnd = dist;
		float proximityScale = 1f - Mathf.Clamp01(dist / rad.radius);

		if (changeFogColor)
		{
			currentFogColor = fogColorGradient.Evaluate(proximityScale);
		}

		if (updateParticles && PartData != null && PartData.JDPartSys != null)
		{
			UpdateParticleParameters(RenderSettings.fogEndDistance, proximityScale);
		}

		EvaluateDistanceZones(dist);
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject != player) return;

		if (JD != null) JD.SetActive(false);
		if (InfectPartSys != null) InfectPartSys.SetActive(false);
		if (nav != null) nav.speed = exitNavSpeed;
		if (anim != null) anim.speed = 1f;

		currentFogEnd = fogDefDistance;
		currentFogColor = fogDefColor;
		isLockedToMaxStage = false;

		if (source != null) source.Stop();
		if (player.TryGetComponent<AudioSource>(out AudioSource playerAudio))
		{
			playerAudio.Play();
		}
	}

	private void ApplyZoneSettings(DistanceState zone)
	{
		if (nav != null)
		{
			nav.speed = navmaxspeed * zone.speedMultiplier;
			if (anim != null) anim.SetFloat("speed", nav.velocity.magnitude / navmaxspeed);
		}
		if (anim != null) anim.speed = zone.animSpeedMultiplier;
		PlayClip(zone.clipIndex);
	}

	private void EvaluateDistanceZones(float currentDistance)
	{
		if (distanceZones == null || distanceZones.Count == 0) return;

		if (isLockedToMaxStage)
		{
			ApplyZoneSettings(distanceZones[distanceZones.Count - 1]);
			return;
		}

		for (int i = 0; i < distanceZones.Count; i++)
		{
			float threshold = rad.radius / distanceZones[i].radiusDivisor;

			if (currentDistance > threshold)
			{
				if (i == distanceZones.Count - 1)
				{
					isLockedToMaxStage = true;
				}
				ApplyZoneSettings(distanceZones[i]);
				return;
			}
		}
	}

	private void UpdateParticleParameters(float activeFogDistance, float proximityScale)
	{
		ParticleSystem ps = PartData.JDPartSys;

		var main = ps.main;
		float targetLifetime = activeFogDistance * lifetimeMultiplier;
		main.startLifetime = Mathf.Min(targetLifetime, maxParticleLifetime);

		var shape = ps.shape;
		float targetRadius = activeFogDistance * particleShapeRadiusMultiplier;
		shape.radius = Mathf.Min(targetRadius, maxParticleRadius);

		var vel = ps.velocityOverLifetime;
		float dynamicVelocity = activeFogDistance * orbitalVelocityMultiplier;
		vel.orbitalX = dynamicVelocity;
		vel.orbitalY = dynamicVelocity;

		var noise = ps.noise;
		float targetNoise = activeFogDistance * noiseStrengthMultiplier;
		noise.strength = Mathf.Min(targetNoise, maxNoiseStrength);

		var emission = ps.emission;
		emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, proximityScale);
	}

	private void PlayClip(int index)
	{
		if (source == null || clips == null || index < 0 || index >= clips.Length) return;

		if (source.clip != clips[index])
		{
			source.Stop();
			source.clip = clips[index];
			source.Play();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!showDebugGizmos || rad == null || distanceZones == null) return;

		Vector3 centerPosition = transform.TransformPoint(rad.center);

		for (int i = 0; i < distanceZones.Count; i++)
		{
			if (distanceZones[i].radiusDivisor <= 0.001f) continue;

			float calculatedRadius = rad.radius / distanceZones[i].radiusDivisor;
			float opacityFactor = 1f - ((float)i / distanceZones.Count * 0.5f);

			if (i == distanceZones.Count - 1)
			{
				Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.6f);
			}
			else
			{
				Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * opacityFactor);
			}
			Gizmos.DrawWireSphere(centerPosition, calculatedRadius);
		}
	}
}
