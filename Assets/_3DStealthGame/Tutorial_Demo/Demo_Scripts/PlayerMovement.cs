using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthGame
{
    public class PlayerMovement : MonoBehaviour
    {
        public InputAction MoveAction;

        public float walkSpeed = 1.0f;
        public float turnSpeed = 20f;

        Animator m_Animator;
        Rigidbody m_Rigidbody;
        AudioSource m_AudioSource;
        Vector3 m_Movement;
        Quaternion m_Rotation = Quaternion.identity;
        FirstPersonController m_FirstPersonController;
    
        // DEMO
        private List<string> m_OwnedKeys = new();

        void Start ()
        {
            m_Animator = GetComponent<Animator> ();
            m_Rigidbody = GetComponent<Rigidbody> ();
            m_AudioSource = GetComponent<AudioSource> ();
            m_FirstPersonController = GetComponent<FirstPersonController>();
        
            MoveAction.Enable();
        }

        void FixedUpdate ()
        {
            bool firstPerson = m_FirstPersonController != null && m_FirstPersonController.isActiveAndEnabled;
            var pos = firstPerson ? m_FirstPersonController.MovementInput : MoveAction.ReadValue<Vector2>();
        
            float horizontal = pos.x;
            float vertical = pos.y;
        
            m_Movement.Set(horizontal, 0f, vertical);
            m_Movement.Normalize ();
            if (firstPerson)
                m_Movement = m_FirstPersonController.BodyRotation * m_Movement;

            bool hasHorizontalInput = !Mathf.Approximately (horizontal, 0f);
            bool hasVerticalInput = !Mathf.Approximately (vertical, 0f);
            bool isWalking = hasHorizontalInput || hasVerticalInput;
            if (m_Animator != null)
                m_Animator.SetBool ("IsWalking", isWalking);
        
            if (m_AudioSource != null && isWalking)
            {
                if (!m_AudioSource.isPlaying)
                {
                    m_AudioSource.Play();
                }
            }
            else if (m_AudioSource != null)
            {
                m_AudioSource.Stop ();
            }

            Vector3 desiredForward = Vector3.RotateTowards (transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
            m_Rotation = firstPerson ? m_FirstPersonController.BodyRotation : Quaternion.LookRotation (desiredForward);
        
            m_Rigidbody.MoveRotation (m_Rotation);
            float speed = walkSpeed;
            m_Rigidbody.MovePosition (m_Rigidbody.position + m_Movement * speed * Time.fixedDeltaTime);
        }

        public void AddKey(string keyName)
        {
            m_OwnedKeys.Add(keyName);
        }

        public bool OwnKey(string keyName)
        {
            return m_OwnedKeys.Contains(keyName);
        }
    }
}
