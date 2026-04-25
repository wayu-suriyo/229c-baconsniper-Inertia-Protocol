#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class WindParticleSetup : EditorWindow
{
    [MenuItem("Tools/Auto Setup Wind Particles")]
    public static void SetupParticles()
    {
        WindZone[] windZones = Object.FindObjectsByType<WindZone>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var wz in windZones)
        {
            if (wz.windParticles != null)
            {
                ParticleSystem ps = wz.windParticles;
                
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                main.startColor = new Color(1f, 1f, 1f, 100f / 255f);

                var emission = ps.emission;
                emission.rateOverTime = 25f;

                var colOverLife = ps.colorOverLifetime;
                colOverLife.enabled = true;
                
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(0.0f, 0.0f), 
                        new GradientAlphaKey(1.0f, 0.5f),
                        new GradientAlphaKey(0.0f, 1.0f) 
                    }
                );
                colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

                // 4. Renderer
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.lengthScale = 3f;
                    renderer.velocityScale = 0.1f; 
                }

                EditorUtility.SetDirty(ps);
                count++;
            }
        }

        Debug.Log($"✅ [WindParticleSetup] Set up Particle for {count} WindZone(s) successfully!");
    }
}
#endif
