using UnityEngine;
using System.Collections;

public class BulletManager : MonoBehaviour {

    public static BulletManager Instance;
    public static GameObject Container;

    Object PlayerBigPrefab;
    Object PlayerBulletPrefab;

    Quaternion BulletRotation = new Quaternion(0, -0.7f, 0, 0.7f); //Default rotation for player fired bullets.
    Plane PlayerBulletPlane;
    Plane EnemyBulletPlane;

    public static BulletManager GetInstance()
    {
        if (!Instance)
        {
            Container = new GameObject();
            Container.name = "BulletManager";
            Instance = Container.AddComponent(typeof(BulletManager)) as BulletManager;
        }
        return Instance;
    }

    public Vector3 GetEnemyBulletSpawnPoint(Vector3 gun)
    {
        Ray ray = new Ray(Camera.main.transform.position, (gun - Camera.main.transform.position).normalized);

        float dist;

        Vector3 newpoint = gun;

        if (EnemyBulletPlane.Raycast(ray, out dist))
        {
            newpoint = ray.GetPoint(dist);
        }

        return newpoint;
    }

    Vector3 GetPlayerBulletSpawnPoint(Vector3 gun)
    {
        Ray ray = new Ray(Camera.main.transform.position, (gun - Camera.main.transform.position).normalized);

        float dist;

        Vector3 newpoint = gun;

        if (PlayerBulletPlane.Raycast(ray, out dist))
        {
            newpoint = ray.GetPoint(dist);
        }

        return newpoint;
    }

    public void BossSpawnShot(Vector3 location, Vector3 velocity)
    {

    }

    public void PlayerBigShot(Vector3 location, Quaternion rotation, bool firedbylocal)
    {
        location = GetPlayerBulletSpawnPoint(location);

        GameObject.Instantiate(PlayerBigPrefab, location, rotation * BulletRotation);
        //PlayerProjectile bscript = bullet.GetComponent<PlayerProjectile>();

    }

    public void PlayerShot(Vector3 location, Quaternion rotation, float speed, float life, bool firedbylocal)
    {
        location = GetPlayerBulletSpawnPoint(location);

        GameObject.Instantiate(PlayerBulletPrefab, location, rotation * BulletRotation);
        
    }

	// Use this for initialization
	void Start () 
    {
        PlayerBigPrefab = Resources.Load("Bullets/PlayerBigShot");
        PlayerBulletPrefab = Resources.Load("Bullets/PlayerBulletPrefab");

        PlayerBulletPlane = new Plane(Vector3.up, new Vector3(0f, 10f, 0f));
        EnemyBulletPlane = new Plane(Vector3.up, new Vector3(0f, 15f, 0f));
	}
	
	// Update is called once per frame
	void Update () 
    {
	
	}
}
