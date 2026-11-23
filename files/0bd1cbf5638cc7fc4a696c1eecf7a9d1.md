# 중정비동 2층 조명 공백 보정 V35

상태: 완료. 적용·저장·재로드·실제 Play 및 독립 검토 결과를 메인이 확인했다.

## 사용자 지시와 기획 의도

사용자가 V34 이후 2층이 지나치게 어둡다고 지적했고, 원인 확인 뒤 `ㄱㄱ`로 보정 적용을 승인했다. 이전 V34는 지붕의 빛 차단을 보강했지만 기존 2층 등기구의 11m 범위가 남측 비교 위치를 덮지 못했다. 바닥·캐릭터·계단 접근의 밝기를 회복하되 창 그림자와 건물 깊이는 유지한다. 전체 노출이나 야외 밝기는 변경하지 않는다.

## 변경 전과 보존 범위

D13 완료 인계 및 clean Edit 상태, 최신 prefab SHA-256 `9CA6AF945E4A2A349C4B71D73D11399D0DE5A638063BC04E6EA57EBC6601B20F`를 확인했다. fresh `before_editor.png`, 실제 Play `before_play_*.png` 8장을 확보했고 `backup/A.unity`, `backup/A.prefab`, `BASELINE.txt`를 남겼다. 이전 V34 화면을 이번 변경 전 자료로 위장하지 않았다.

D12 도로/수변, D13 외곽 울타리, 기존 원본 재질·메시·배치·충돌·빛 차단·V34 광원/전역광/후처리·진입/탈출·캐릭터를 보존한다. B4/R8 간격과 비활성 위험 진입 정책을 수정하는 작업이 아니며 해당 문제를 해결했다고 주장하지 않는다.

## 분담과 방법

메인이 새 V35 워크플로·실제 Unity 실행/촬영/이동 시험·통합을 맡는다. GPT-6 Sol high 제작 담당은 신규 `StageAHallV35Lighting.cs`, 독립 검증 담당은 신규 `StageAHallV35Validation.cs`만 맡는다. 새 내용은 `V13_HallRoot/Floor_2F/V35_2FLighting` 아래에 한정한다. 기존 층 가림은 Renderer만 숨기고 Light는 끄지 않으므로 1층에서 2층 등기구가 허공에 보이는 것을 방지한다. 공유 Unity는 메인만 사용한다.

## 화면 대응과 확인 계획

`before_play_<이름>.png`와 최종 `after_play_<이름>.png`를 비교한다. 이름: south_west, south_east, stair_west, stair_east, north, first_floor, outside, remote. 게임 카메라 FOV 60/1600×1000/HUD 제외/전투 정지/동일 위치 방향. 좌표는 PLAY.txt에 기록한다. 캐릭터 대기 애니메이션 프레임은 미세하게 다를 수 있다. Computer Use 스킬로 실제 Unity 창 상태를 별도 촬영한다.

원본 보존 검증, 남측/계단 밝기의 독립 시각 검토, 1층/야외/비대상 구역 영향, 층 가림, 실제 PlayerMotor의 서·동 계단 상하행과 2층 동선, 저장·재로드를 확인한다. FPS 성능 보증은 하지 않는다. 적용 수량/후보/최종 결과/주요 파일은 완료 후 덧붙인다.

## 후보 조정 이력

- 후보01: 남측 강도 24 / 계단 강도 16, 범위 8.5m, 외각 110° / 내각 70°. 기존 등기구 복제 4개와 Spot 4개, 관리 루트 1개로 총 9개 오브젝트를 추가했다. 원본 보존 16항목 및 실제 PlayerMotor 8경로는 PASS였으나, 밝은 풀이 좁아 넓은 바닥·계단 주변의 어두움을 충분히 줄이지 못했다는 독립 검토에 따라 시각적 최종 합격을 보류했다. 후보01 자료를 삭제하거나 최종 결과로 바꾸지 않았다.
- 후보02: 같은 신규 4개만 강도 남측 36 / 계단 24, 범위 10m, 외각 145° / 내각 110°로 조정해 바닥으로 퍼지는 빛을 넓혔다. 기존 5개 등기구와 광원, 전체 노출은 변경하지 않았다. 광원 높이 12.43m에서 1층 바닥 2m까지 약 10.43m이므로 범위 10m는 평평한 1층 바닥에 직접 닿지 않는다. 단, 이 계산만으로 벽·계단 누출까지 보증하지 않으며 대응 화면으로 확인한다.
- 층 가림: 1층에서 신규 2층 등기구 Renderer 0/4, 2층에서 4/4. 외부에서는 기존 층 표시 규칙상 enabled 4/4이며 지붕 뒤에 가려진 상태다. '외부에서 자동 비활성화'라고 주장하지 않는다.

## 후보02 확인 결과

- `VALIDATION_candidate02.txt`: 기술 검사 16 PASS / 0 FAIL. D13 기준 원본 오브젝트 33,892개는 누락·구조·외형 차이 0개, 기존 참조 자산/의존 파일 4,485개 변경 0개. 기존 Light/Volume 16개 및 전역 렌더링 설정 유지, V34 그림자 전용 메시 81개 유지. 신규 오브젝트 9개는 V35 루트 안에만 존재하며 충돌체 0개다.
- `candidate02_PLAY.txt`: 동일 카메라/좌표 8시점, 층 가림, 지역 톤매핑 모드 연속성 PASS. 실제 PlayerMotor로 서·동 계단 상행/하행 4경로와 2층 계단↔남측 왕복 4경로, 총 8경로 PASS. 경로 시작점으로만 배치하고 이후에는 CharacterController를 켠 실제 이동을 사용했다. 임의의 모든 문·모든 구간을 검증한 것은 아니다.
- GPT-6 Sol high 독립 시각 검토: 남서·남동 바닥과 서·동 계단 상부 접근로에 한해 시각 합격 가능. 후보01보다 빛이 넓고 부드러우며 바닥과 캐릭터, 계단 입구가 더 잘 읽힌다. 창 그림자가 남고 북측/1층/외부/원격 대응 화면은 실질적으로 동일하다. 최종 승인은 저장·재로드·최종 Play 확인 후 기록한다.
- 한계: 2층 전체의 모든 암부와 계단 아래쪽을 밝게 바꾼 작업이 아니다. 탑뷰에서 천장을 가릴 때 기존 형태의 등기구가 회색 원형으로 보이는 표현은 유지한다. 전투 정지 캡처이며 FPS 벤치마크는 하지 않았다. 지역 톤매핑 검사는 enum 모드 확인이지 모든 경계의 픽셀 밝기 연속성을 증명하는 검사는 아니다.

## 주요 파일

- 실제 내용 수정: `C:/UnityProject/escape/Assets/Escape/Resources/StageMaps/StageA_Relay_Blockout_v08.prefab`. 연결된 `C:/UnityProject/escape/Assets/Escape/Scenes/StageA_Relay_Blockout_v08.unity`도 저장했으며, 프리팹 내부 변경만 있으므로 씬 파일 해시는 변경 전과 동일하다. 저장 완료 해시는 `SAVED.txt`에 기록했다.
- 신규 Editor 도구: `StageAHallV35Lighting.cs`, `StageAHallV35Validation.cs`, `StageAHallV35Workflow.cs`, `StageAHallV35Play.cs` 및 Unity가 생성한 대응 meta. 모두 `C:/UnityProject/escape/Assets/Escape/Editor/` 안에 있다.
- 기존 등기구 메시/재질을 재사용했으며 신규 커스텀 런타임 스크립트, 텍스처, 재질, Collider를 추가하지 않았다. 추가 컴포넌트는 Transform, MeshFilter/MeshRenderer, Unity Light 및 URP AdditionalLightData로 한정했다.

## 저장 및 재검증

`VALIDATION_presave.txt` 16/16 PASS 뒤 씬·프리팹을 저장하고, clean 씬을 재로드했다(`COMMAND_save.txt`, `COMMAND_reload.txt` COMPLETE). 재로드된 데이터로 최종 `after_play_*.png` 8장을 촬영하고 `after_PLAY.txt`의 8개 실제 이동 경로 및 층 가림 검사가 다시 PASS했다. 비교 위치·카메라·FOV·출력 해상도는 before와 같다.

- 최종 prefab SHA-256: `222ABE5B6D8094ED6E1FC06AC5A22B2A1F79CE8BBA2B02A75D36F4DE1879B602`
- 씬 SHA-256: `C9E3D324A6DA7849188D1D92F9D79843B8E4820DCBE8325C3A0B227CD13D1874`
- `VALIDATION_reloaded.txt`: 재로드 후에도 16 PASS / 0 FAIL. 원본 오브젝트·자산·기존 광원/후처리·충돌 보존 유지.
- `COMMAND_status.txt`: `playing=False`, `dirty=False`, retained A scene 확인. `after_editor.png`는 Computer Use로 실제 Unity의 정지·저장된 편집 화면을 촬영한 증빙이며, 이 전체 맵 화면에서는 실내 조명 차이가 직접 드러나지 않는다. 실내 효과 비교는 대응하는 실제 Play 전후 8쌍으로 한다.
- 최종 해시를 저장 직후 값과 다시 대조했으며 일치했다. 시작 백업과 최종 대응 화면 및 이 기록을 보존했다.
- 최종 `INDEPENDENT_REVIEW.md`를 메인이 직접 읽고 통합 승인했다. 기술 검사와 시각 검사를 분리해 판정했으며, 최종 수락 범위는 남측 및 서·동 계단 상부 접근로의 국소 암부 완화다. 2층 전체가 균일하게 밝아졌거나 전체 그래픽 목표가 완성됐다고 주장하지 않는다.
- 공유 Unity 편집·저장·Play 검증을 종료했다. 최종 대기 상태는 clean Edit이며, 다른 맵 작업으로 최신 프리팹 해시와 작업 종료를 인계한다.
