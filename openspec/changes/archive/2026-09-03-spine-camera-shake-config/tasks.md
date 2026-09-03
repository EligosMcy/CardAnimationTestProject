# 任务清单:spine-camera-shake-config

## 1. 新增 SpineBattleSettings 配置资产

- [x] 1.1 新建 `Assets/SpineTest/Battle/Scripts/SpineBattleSettings.cs`:`ScriptableObject` + `[CreateAssetMenu]`,字段分组:节拍(`AttackStartTime=0.7`/`AttackFreezeFrame=0.75`/`HitFrameTime=0.3`)、表现与回位(`PresentationDuration=0.6`/`ReturnDuration=0.15`)、缩放(`FocusScaleMultiplier=2`/`GroupScaleMultiplier=1.1`)、相机晃动(`CameraShakeAmplitude≈0.05`/`CameraShakeSpeed≈25`/`CameraShakeRotationAmplitude=0` 预留)、`AttackClipName="Attack1"`、`EnableDebugKeys=true`;默认值字面量与当前场景数据一致
- [x] 1.2 创建资产 `Assets/SpineTest/Battle/Settings/SpineBattleCinematicSettings.asset`(数值与 1.1 默认一致)

## 2. BattleStage:删除平移/演出对晃动,新增相机晃动

- [x] 2.1 删除 `_focusGroup` 引用、`_groupShiftDistance/_groupShiftDuration/_groupShiftCurve` 与平移协程/组回位逻辑;删除 `_shakeOffsetAmplitude/_shakeScaleAmplitude/_shakeSpeed/_shakeStartWithShift` 与晃动协程;`SnapFocusIn` 不再记录/归位组位置
- [x] 2.2 新增 `[SerializeField] Transform _demoCamera`(场景 BattleDemo Camera)与相机基准记录;新增 `StartCameraShake()`(表现窗口内对相机做小幅噪声位移,参数来自 `_settings`)与 `StopCameraShake()`(停止并精确还原相机基准)
- [x] 2.3 `RecoverPresentation` 改为:停止相机晃动并还原 → `ZoomGroupOut()` → `ReturnHome`(缩小回 Home 并还原,完成回调不变);移除"组回原位"段
- [x] 2.4 `tryValidateRefs` 增加 `_settings == null`、`_demoCamera == null` 校验;运行期参数读 `_settings`(倍率/回位时长等)

## 3. SpineBattleDirector:参数改读 SO 并驱动相机晃动

- [x] 3.1 删除 `_attackStartTime/_attackFreezeFrame/_hitFrameTime/_presentationDuration/_returnDuration/_attackClipName/_enableDebugKeys` 内联字段,新增 `[SerializeField] SpineBattleSettings _settings`,读值点全部改为 `_settings.*`;`tryValidateRefs` 增加 `_settings == null` 校验(启动即报错中断)
- [x] 3.2 打击节拍:双定格/瞬时挂载放大/组与背景放大/幕布开保持,调用 `_stage.StartCameraShake()` 替代原平移+晃动启动;表现窗口计时仍由 Director 驱动(`_settings.PresentationDuration`)
- [x] 3.3 恢复节拍与收尾时序保持 `fix-spine-resume-after-return` 语义(回位完成后续播);`_settings.ReturnDuration` 传参

## 4. 场景接线与清理

- [x] 4.1 场景中 BattleStage/Director 组件接入 `SpineBattleCinematicSettings.asset` 与 `_demoCamera`;保存场景使旧数值字段(平移/晃动/内联时长等)随重存清除
- [x] 4.2 确认无需再引用 FoucsAnchorGroup 平移相关对象引用;运行初始化无 Error 日志

## 5. 人工验收(用户在编辑器执行)

- [x] 5.1 编译零错误;按 B 触发演出:表现窗口内相机小幅晃动、结束即停且精确还原基准;演出对不再平移/自晃;组/背景放大、幕布、双定格、回位后续播等既有表现正常
- [x] 5.2 只修改 `SpineBattleCinematicSettings.asset` 单文件(如晃动幅度/速度、表现窗口、倍率)后,重进 Play 即可整体调整演出效果;确认组件 Inspector 无残留重复参数
