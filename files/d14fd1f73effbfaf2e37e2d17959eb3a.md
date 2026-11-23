# V31 A맵 자연광 제작 및 시각 검토

## 목적과 적용 범위

A맵의 직사광과 하늘 반사광을 조금 더 구별되게 조율했다. 기존 화면의 그림자가 이미 충분히 어두운 곳이 있어 주변광을 크게 줄이지 않았고, 장면 전체 노출을 높이지 않았다. A맵 진입 시 사용하는 기본 조명과 A맵 편집 씬의 미리보기 조명을 같은 값으로 맞췄다. 중앙 공장 V16 구역은 기존 28m 연속 보간을 유지하면서 기본 조명과 가까운 지역 목표값으로 연결했다.

| 항목 | 변경 전 A맵 기본값 | V31 값 |
| --- | --- | --- |
| 하늘 주변광 | (0.55, 0.60, 0.65) | (0.48, 0.55, 0.62) |
| 수평선 주변광 | (0.40, 0.44, 0.48) | (0.34, 0.39, 0.44) |
| 지면 반사광 | (0.25, 0.28, 0.30) | (0.18, 0.21, 0.24) |
| 직사광 색/세기 | (0.93, 0.96, 1.00) × 1.4 | (0.99, 0.97, 0.93) × 1.5 |

V16 구역 목표값은 하늘 (0.51, 0.56, 0.62), 수평선 (0.35, 0.40, 0.44), 지면 (0.19, 0.21, 0.23), 직사광 (1.00, 0.97, 0.92) × 1.5다. 태양 방향, 그림자 방식·강도·거리, 지역 Volume의 Tonemapping.mode overrideState=false, 노출·Bloom 값, 반사 프로브, 공용 URP 에셋은 변경하지 않았다. 등기구 근거가 없는 점광원도 추가하지 않았다.

## 수정 파일

- `C:/UnityProject/escape/Assets/Escape/Runtime/Stage/Blockout/StageAGraphicsV31Environment.cs`: A맵 편집 및 게임이 공유하는 조명 팔레트.
- `C:/UnityProject/escape/Assets/Escape/Runtime/Stage/Blockout/StageMapRouter.cs`: A맵 진입 시 기본 조명만 새 팔레트로 설정. B맵 복구·스폰·내비게이션 로직은 그대로 유지.
- `C:/UnityProject/escape/Assets/Escape/Runtime/Stage/Blockout/StageALookV16Atmosphere.cs`: 중앙 구역의 지역 목표색을 기본 팔레트와 조화되게 조정. Weight 계산·경계 보간·종료 시 복구는 그대로 유지.
- `C:/UnityProject/escape/Assets/Escape/Editor/StageAGraphicsV31Lighting.cs`: 편집 중인 A씬의 `Edit overview light`와 주변광만 맞추는 `Apply()` 함수. 씬 저장은 호출자가 담당.

## 같은 시점 전후 화면 검토

- `before_play_factory_1f.png` ↔ `after_play_factory_1f.png`: 바닥과 벽의 광량 변화는 작다. 창과 벽이 만드는 그림자 형태 및 실내 가독성이 유지되고, 바닥의 밝은 면과 그늘 면 구분이 약간 선명해졌다. 큰 밝기 증가나 새로운 광원으로 보이지 않는다.
- `before_play_factory_2f.png` ↔ `after_play_factory_2f.png`: 2층 바닥과 창 그림자에서 미세한 대비 변화가 보인다. 2층의 어두운 벽 가장자리와 통로 가독성은 유지된다.
- `before_play_sc.png` ↔ `after_play_sc.png`, `before_play_west_d02.png` ↔ `after_play_west_d02.png`: D03 및 D02 외곽 화면의 차이는 매우 작다. 건물·컨테이너·경로의 배치와 가시성은 같으며, 이 화면들만으로 극적인 사실감 향상을 주장할 수 없다.
- `before_outdoor_factory.png` ↔ `after_outdoor_factory.png`: 고고도 편집 화면은 두 번째 촬영에서 동일한 건물 표시 상태로 대응된다. 게임 카메라보다 훨씬 높아 50m 그림자 거리 밖의 구조물이 많으므로 게임 그림자 품질의 근거로 사용하지 않는다.

첫 `after_outdoor_factory.png`에는 여러 건물과 공장 지붕이 누락되어 전후 비교 자료로 부적합했다. 첫 `after_PLAY_VALIDATION.txt`에서도 최초 동쪽 경계 샘플에만 큰 렌더 차이가 발생했다. 이후 렌더 상태를 다시 정리하고 첫 프레임을 예열한 재촬영에서 건물 표시가 정상화됐으며 네 경계가 모두 통과했다. 동일한 초기 렌더 문제가 다시 생기지 않는지는 저장 후 재로드·새 플레이로 한 번 더 확인해야 한다. 초기 이상이 조명 수치 때문이었다고 단정하지 않는다.

## 현재 확인 결과와 한계

- 재촬영된 `after_PLAY_VALIDATION.txt`: 동·서·북·남 경계의 즉시 평균 휘도 차이가 모두 약 0.000001, Neutral 톤매핑 연속성 및 B맵 주변광·태양·안개 복구 PASS.
- `after_DISTRICT_VALIDATION.txt`: D02 서쪽과 D03 SC에서 중앙 지역 Volume의 Weight=0 확인 PASS.
- `GEOMETRY_VALIDATION.txt`: 보호한 변환·메시·콜라이더 차이 0, PASS.
- 조명 변경의 체감 폭은 의도적으로 보수적이다. 기존 반사 프로브, 태양 그림자 거리와 간접광 방식을 유지했으므로 맵 전체의 물리 기반 간접조명이나 반사 정확도가 근본적으로 바뀐 것은 아니다. 표면의 노멀·금속 응답 변경은 별도 재질 작업의 결과이며 이 조명 수치만의 효과로 간주하지 않는다.

최종 저장본을 새로 연 실제 플레이의 컴파일·렌더·밝기 경계 검증은 메인 통합 검사 결과를 확인해야 한다.

최종 통합 추기: 18:23 저장·재열기 후 18:23:59 시작한 실제 Play 재검사도 네 경계 모두 약 .000001, defects=0 PASS였다. 셰이더 오류 0과 Play 종료를 확인했다. 첫 실패는 그대로 보존하며, 최종 결과는 CHANGE_RECORD.md와 INDEPENDENT_REVIEW.md에 기록했다.
