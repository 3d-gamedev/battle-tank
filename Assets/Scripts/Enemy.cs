// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using UnityEngine;
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;

namespace GameDev {
    /// <summary>
    /// enemy script.
    /// </summary>
    public class Enemy : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] GameObject _bullet; // 弾のプレハブを設定します

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        float SEARCH_ANGLE = 60.0f; // 索敵アングル(※実際の範囲は2倍になります)

        GameObject _player_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        public bool NotPiece { get => !name.Contains(value: "_Piece"); } // 破片ではないフラグ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _player_object = Find(name: "Player"); // Player オブジェクトの参照を取得
        }

        // Start is called before the first frame update.
        void Start() {
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
            /// 索敵中に回転します
            /// </summary>
            bool searching = true; // 索敵中フラグ
            bool rotate = false; // 回転中フラグ
            bool stop = false; // 停止フラグ
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    searching && !rotate && // 索敵中フラグONかつ回転中ではない場合
                    NotPiece)
                .Subscribe(onNext: _ => {
                    // ランダムで移動位置指定
                    int random1 = Mathf.FloorToInt(f: Random.Range(minInclusive: 0.0f, maxInclusive: 8.0f));
                    int random2 = Mathf.FloorToInt(f: Random.Range(minInclusive: 0.1f, maxInclusive: 4.0f));
                    int random3 = Mathf.FloorToInt(f: Random.Range(minInclusive: -3.0f, maxInclusive: 3.0f));
                    int random4 = Mathf.FloorToInt(f: Random.Range(minInclusive: -3.0f, maxInclusive: 3.0f));
                    rotate = true; // 回転中フラグON
                    // random1 秒後に
                    Observable.Timer(System.TimeSpan.FromSeconds(value: random1))
                        .Subscribe(onNext: _ => {
                            stop = true; // 一時停止して
                            // さらに random2 秒後に
                            Observable.Timer(System.TimeSpan.FromSeconds(value: random2))
                                .Subscribe(onNext: _ => {
                                    int random5 = Mathf.FloorToInt(f: Random.Range(minInclusive: 1.0f, maxInclusive: 6.0f));
                                    if (random5 % 2 == 1) { // 偶数なら
                                        // ランダムな方向を向く
                                        transform.LookAt(worldPosition: new Vector3(
                                            x: transform.position.x + random3,
                                            y: transform.position.y,
                                            z: transform.position.z + random4
                                        ));
                                        stop = false; rotate = false;
                                    } else { // 奇数ならそのままの方向を維持
                                        stop = false; rotate = false;
                                    }
                            }).AddTo(gameObjectComponent: this);
                        }).AddTo(gameObjectComponent: this);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 一時停止します
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    stop && // 停止フラグONの場合
                    NotPiece)
                .Subscribe(onNext: _ => {
                    rb.velocity = Vector3.zero;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 索敵中に移動します
            /// </summary>
            float SPEED_LIMIT = 3.0f; // スピード制限フラグ
            float FORWARD_FORCE = 16.0f; // 前進力
            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    searching && !stop && // 索敵中フラグONかつ停止中でない場合
                    NotPiece)
                .Subscribe(onNext: _ => {
                    if (speed < SPEED_LIMIT) { // 速度リミットまで
                        rb.AddFor​​ce(force: transform.forward * FORWARD_FORCE, mode: ForceMode.Acceleration); // 前に力を加える
                    }
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 追跡中に回転します
            /// </summary>
            bool chasing = false; // 追跡中フラグ
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    chasing && // 追跡中フラグONの場合
                    NotPiece)
                .Subscribe(onNext: _ => {
                    // Player オブジェクトの方向に回転する
                    transform.LookAt(worldPosition: new Vector3(
                        x: _player_object.transform.position.x,
                        y: transform.position.y,
                        z: _player_object.transform.position.z
                    ));
                    shoot(); // 弾を撃つ
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 追跡中に異動します
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    chasing && // 追跡中フラグONの場合
                    NotPiece)
                .Subscribe(onNext: _ => {
                    if (speed < SPEED_LIMIT) { // 速度リミットまで
                        rb.AddFor​​ce(force: transform.forward * FORWARD_FORCE, mode: ForceMode.Acceleration); // 前に力を加える
                    }
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 索敵用コライダーに接触したとき
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x => 
                    x.name.Equals("Player"))
                .Subscribe(onNext: _ => {
                    chasing = discovered(target: _player_object); // 追跡フラグ判定
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 索敵用コライダーとの接触を維持しているとき
            /// </summary>
            this.OnTriggerStayAsObservable()
                .Where(predicate: x => 
                    x.name.Equals("Player"))
                .Subscribe(onNext: _ => {
                    chasing = discovered(target: _player_object); // 追跡フラグ判定
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 索敵用コライダーから離脱したとき
            /// </summary>
            this.OnTriggerExitAsObservable()
                .Where(predicate: x => 
                    x.name.Equals("Player"))
                .Subscribe(onNext: _ => {
                    chasing = false; // 追跡フラグOFF
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// 追跡フラグを判定します (※視界の角度内に収まっているか)
        /// </summary>
        bool discovered(GameObject target) {
            // 対象までのベクトルを取得
            Vector3 position_delta = target.transform.position - transform.position;
            // 前方ベクトルと対象までの角度を取得
            float target_angle = Vector3.Angle(from: transform.forward, to: position_delta);
            // 視界内に収まっている場合
            if (target_angle < SEARCH_ANGLE) { 
                return true; // 追跡フラグON
            }
            return false; // 追跡フラグOFF
        }

        /// <summary>
        /// 弾を撃ちます
        /// </summary>
        void shoot() {
            float BULLET_SPEED = 7500.0f; // 弾の速度

            // ランダム値を生成して3の時だけ弾を発射
            int random = Mathf.FloorToInt(f: Random.Range(minInclusive: 0.0f, maxInclusive: 1024.0f));
            if (random == 3.0f) {
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
}