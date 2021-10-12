using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//may need to be refactored to be a parent class to all projectiles
//current just for elias' shot
public class Projectile : MonoBehaviour
{
    //need to either rotate sprite or use an orb sprite and work out particle effects
    //needs to be destroyed after x amount of time
    //needs to deal damage and destroy when hit something (not the player)

    public Vector3 direction; //direction to move in
    public float speed = .2f;
    public string targetTag;
    public float damage;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Lifetime());
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += direction * speed;
    }
    

    IEnumerator Lifetime()
    {
        yield return new WaitForSeconds(10); // can probably be less
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == targetTag)
        {
            Debug.Log("projectile hit something!");
            //deal damage
            if(collision.tag == "playerHitbox")
            {
                collision.GetComponentInParent<PlayerScript>().ChangePlayerHealth(damage, "hit");
            }
            else if(collision.tag == "EnemyHitbox")
            {
                Debug.Log("projectile hit an enemy!");
                Debug.Log(collision.GetComponentInParent<Enemy>());
                collision.GetComponentInParent<Enemy>().TakeDamage(damage);
            }
            Destroy(gameObject, 0.1f);
        }
    }
}
