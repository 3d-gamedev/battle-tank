// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE text in the project root for license information.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UniRx;
using UniRx.Triggers;

namespace GameDev {
    /// <summary>
    /// player script.
    /// </summary>
    public class Player : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] GameObject _bullet; // 弾のプレハブを設定します

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        ButtonControl _a_button, _b_button, _up_button, _down_button, _left_button, _right_button;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        void Start() {
            /// <summary>
            /// キー入力を取得します
            /// </summary>
            this.UpdateAsObservable()
                .Subscribe(onNext: _ => {
                    mapGamepad();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Rigidbody コンポーネントを取得します
            /// </summary>
            Rigidbody rb = transform.GetComponent<Rigidbody>();

            /// <summary>
            /// 速度を取得します
            /// </summary>
            float speed = 0f;
            this.FixedUpdateAsObservable()
                .Subscribe(onNext: _ => {
                    speed = rb.velocity.magnitude;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 前進します
            /// </summary>
            bool forward = false; // 前進フラグ
            float SPEED_LIMIT = 5.0f; // スピード制限フラグ
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _b_button.isPressed && // 上ボタンが押されて かつ
                    speed < SPEED_LIMIT) // スピード制限以下の時
                .Subscribe(onNext: _ => {
                    forward = true; // 前進フラグをONにする
                }).AddTo(gameObjectComponent: this);

            float FORWARD_FORCE = 7.5f; // 前進力
            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    forward) // 前進フラグがONの場合
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 10.0f; // 調整値
                    rb.AddFor​​ce( // Rigidbody コンポーネントに力を加えます
                        force: transform.forward * FORWARD_FORCE * ADJUST_VALUE, // 力のベクトル
                        mode: ForceMode.Acceleration // 力をかけるモード: アクセラレーション
                    );
                    forward = false; // 前進フラグをOFFにする
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 回転します
            /// </summary>
            float ROTATIONAL_SPEED = 7.5f; // 回転スピード
            this.UpdateAsObservable()
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 10.0f; // 調整値
                    int axis = _right_button.isPressed ? // 右ボタンが押されているか？
                        1 : // Yes: axis は 1
                        _left_button.isPressed ? // No: 左ボタンが押されているか？
                            -1 : // Yes: axis は -1
                            0; // No: axis は 0 (※左右キーは押されていない)
                    transform.Rotate( // オブジェクトを回転します
                        xAngle: 0, 
                        yAngle: axis * (ROTATIONAL_SPEED * Time.deltaTime) * ADJUST_VALUE, // Y軸(ヨー)で回転する
                        zAngle: 0
                    );
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 弾を撃ちます
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ =>  
                    _a_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    shoot(); // 弾を撃つ
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// キー入力を取得します
        /// </summary>
        void mapGamepad() {
            // PCのキーボード入力を取得
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
                _up_button = Keyboard.current.upArrowKey; // キーボードの上カーソルキー
                _down_button = Keyboard.current.downArrowKey; // キーボードの下カーソルキー
                _left_button = Keyboard.current.leftArrowKey; // キーボードの左カーソルキー
                _right_button = Keyboard.current.rightArrowKey; // キーボードの右カーソルキー
                _b_button = Keyboard.current.zKey; // キーボードのZキー
                _a_button = Keyboard.current.xKey; // キーボードのXキー
                return;
            }
        }

        /// <summary>
        /// 弾を撃ちます
        /// </summary>
        void shoot() {
            float BULLET_SPEED = 7500.0f; // 弾の速度

            // 弾の複製
            GameObject bullet = Instantiate(original: _bullet);

            // 弾の位置
            Vector3 position = transform.position + (transform.forward * 1.0f); // ノズル前方
            bullet.transform.position = position;

            // 弾へ加える力
            Vector3 force = (transform.forward + Vector3.up * 0.01f) * BULLET_SPEED; // ごくわずかに上向き

            // 弾を発射
            bullet.GetComponent<Rigidbody>().AddForce(force: force, mode: ForceMode.Force);
        }
    }
}