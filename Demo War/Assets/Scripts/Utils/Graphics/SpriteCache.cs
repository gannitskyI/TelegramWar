using System.Collections.Generic;
using UnityEngine;

public static class SpriteCache
{
    private static readonly Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
    private static bool isInitialized = false;

    public static void Initialize()
    {
        if (isInitialized) return;

        CreateEnemySprites();
        CreateProjectileSprites();
        CreateExperienceSprite();
        CreateBulletSprites();

        isInitialized = true;
    }

    private static void CreateEnemySprites()
    {
        cachedSprites["enemy_basic"] = CreateCircleSprite(32, Color.red);
        cachedSprites["enemy_elite"] = CreateCircleSprite(40, Color.magenta);
        cachedSprites["enemy_boss"] = CreateCircleSprite(64, Color.black);
    }

    private static void CreateProjectileSprites()
    {
        cachedSprites["projectile_basic"] = CreateCircleSprite(12, Color.yellow);
        cachedSprites["projectile_homing"] = CreateCircleSprite(16, Color.magenta);
        cachedSprites["projectile_explosive"] = CreateCircleSprite(20, Color.red);
    }

    private static void CreateExperienceSprite()
    {
        cachedSprites["experience"] = CreateCircleSprite(32, new Color(0.2f, 1f, 0.3f, 1f));
    }

    private static void CreateBulletSprites()
    {
        cachedSprites["bullet_player"] = CreateCircleSprite(16, Color.cyan);
        cachedSprites["bullet_enemy"] = CreateCircleSprite(14, Color.orange);
    }

    private static Sprite CreateCircleSprite(int size, Color color)
    {
        var texture = new Texture2D(size, size);
        var colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < radius)
                {
                    float alpha = 1f - (distance / radius) * 0.3f;
                    colors[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Sprite GetSprite(string key)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        return cachedSprites.TryGetValue(key, out var sprite) ? sprite : null;
    }

    public static void Cleanup()
    {
        foreach (var sprite in cachedSprites.Values)
        {
            if (sprite != null && sprite.texture != null)
            {
                Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            }
        }

        cachedSprites.Clear();
        isInitialized = false;
    }
}