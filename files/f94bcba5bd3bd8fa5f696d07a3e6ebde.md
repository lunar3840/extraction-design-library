# A맵 기계 도장 채도 완화

2026-10-16. Unity 도장 적용·전후 촬영·보호 검사·독립 검토 완료.

## 사용자 지시와 의도

“너무 튀는 색상으로 설정하진 말고, 적당히 게임 디자인에 위배하지 않는 어울리는 색상 정도로만 부탁함.”

직전 7종 외형 다양화에서 녹색·파랑·청록 도장이 A맵의 산업 시설/금속/마모 표현보다 강하게 보였다. 서로 다른 차체·상체·무기는 유지하고, 도장만 황토·산화 적색·회백·올리브·회청·짙은 청록·차콜 계열로 낮춘다. 색을 공격 속성이나 약점 시스템으로 확대하지 않는다.

메인이 조정·실행·통합을 맡고 GPT-6 Sol high 검토자에게 도장 원인과 최종 화면의 독립 검토를 분담한다. 검토자는 Unity/에셋을 수정하지 않는다.

## 변경 전 상태와 증빙

직전 시안의 RGB 배율 Green (.62,1.25,.62), Blue (.42,.85,1.75), Teal (.50,1.35,1.40)가 일부 채널을 과하게 강화했다. 현재 공유 재질8개·재생성 코드와 소스/맵 해시를 `baseline/`, `BASELINE_SOURCE_HASHES.txt`, `BASELINE_MAP_HASHES.txt`에 보존했다.

도장 수정 전에 현재 모델을 새로 촬영했다. `catalog/before_*`와 `after_*`는 동일 조명·카메라·공통 배율, 실제 A맵 `before_detail_*`와 `after_detail_*` 7쌍 및 `named/` 4쌍은 같은 대상/좌표/카메라를 사용한다. 모든 전 화면은 수정 전 현재 상태이며 재구성 이미지가 아니다. HUD 시간·플레이어 무작위 진입에 따른 미니맵 차이는 있을 수 있다.

## 실제 변경과 방식

`StageAMachinePalettesV2.cs`에서 밝은 Diffuse 대신 보유 패키지의 `Materials/Realistic/MC_Spiders_Albedo_{색}.png` 및 `MC_Spiders_Camo_Albedo_{색}.png`를 공유 URP/Lit 재질8개에 연결했다. 원본 텍스처를 수정하거나 새 텍스처를 생성하지 않았다. `_BaseMap`/`_MainTex`를 함께 연결하고, `_BaseColor`/`_Color`의 모든 RGB 채널을 1 이하로 낮췄다. 금속성·법선·거칠기·마모·별도 트랙·렌즈는 보존했다.

| 역할 | 최종 도장 인상 | RGB 배율 |
| --- | --- | --- |
| Scout / N4 | 어두운 황토 도장 | .94 / .91 / .85 |
| Suppressor | 산화 적색/녹슨 붉은 판금 | .94 / .89 / .86 |
| Sniper / N3 | 마모된 회백색 | .94 / .94 / .90 |
| Grenadier / N2 | 어두운 카키/올리브 | .93 / .97 / .90 |
| Discharger | 짙은 회청색 | .88 / .95 / 1.00 |
| ArcRifle | 회녹색 위장, 청록은 약한 색조 | .90 / .98 / .96 |
| GuardTank | 차콜/검은 판금 | 1 / 1 / 1 |
| N5 | 어두운 적색 위장 | .95 / .91 / .88 |

실전 조명에서는 Grenadier가 카키·황토 쪽으로, ArcRifle은 회녹색으로 보인다. 원색 7개를 강하게 드러내는 방식에서 차체·무장의 윤곽으로 먼저 구분하고, 도장 색은 보조하는 방향으로 조정했다.

## 검증 결과와 증빙

- 신규 수정 전/후 카탈로그 색·무채색16쌍, 실제 A맵 일반7쌍·네임드4쌍을 저장했다. 총27쌍/54장이다. `SCREENSHOT_MANIFEST.csv`에 대응 파일·해상도·SHA256을 기록한다.
- `COMMAND_after_protected.txt`와 복사한 `IDENTITY_AFTER_STATUS.txt` PASS. 73슬롯/보호 객체/씬·맵/기존 네임드 몸체/명시한 N3 controller 예외가 보존됐으며, 7종 도장·크기·총구 검사를 통과했다. `after_detail_status.txt`와 `named/after_detail_status.txt` 모두 PASS.
- `BASELINE_SOURCE_HASHES.txt`의 직전 완료본92개 파일과 비교해 바뀐 기존 파일은 공유 재질8개와 재생성 코드1개뿐이다(`CHANGED_SOURCE_FILES.txt`). 모델 프리팹·애니메이션·컨트롤러·Runtime AI·공격 패턴 파일은 동일하다. 촬영 전용 `StageAMachineMuted{Catalog,Details,Named}.cs` 3개는 추가된 Editor 진단 도구다.
- 직전 외형 작업의 3변형 주행/전투/성능 검증은 `../a-map-machine-silhouette-palette-v2/CHANGE_RECORD.md`와 해당 로그에 보존한다. 이번 재질만의 후속 조정에서 전투·주행 검사를 불필요하게 반복하지 않았다. 도장 공유 방식·Renderer·Animator 수는 동일하다.
- 메인이 카탈로그·실제 야외/건물 주변 상세·네임드 화면 및 독립 보고서 전체를 읽고 통합했다. 독립 최종 판정은 `INDEPENDENT_REVIEW.md`의 **수용 PASS**다. 검토자가 실제 일반7쌍·네임드4쌍과 카탈로그를 확인했다. 원거리 색만으로 구분이 보장된다고 주장하지 않는다.
- 최종 소스 사본은 `final/`, 목록/해시는 `FINAL_SOURCE_HASHES.txt`, 저장 상태는 `FINAL_EDITOR_STATUS.txt`, 증빙 확인은 `EVIDENCE_CHECK.txt`에 저장한다.

17:47:39 KST 최종 저장 상태는 A맵 Edit/dirty False/enemies 0이며, 같은 상태와 해시를 전달하고 공유 Unity를 수면·강우 작업 채팅에 인계했다. 이후 이 작업에서는 Unity/Assets를 변경하지 않았다. 최종 증빙27쌍·CHANGE_RECORD·독립 검토 문서의 존재를 확인했다.

후속 수면·강우 작업의 SaveAssets 이후 Green/Teal 재질에 소수점 직렬화 차이가 보고됐다. 이쪽에서도 보관된 `final/`과 현재 파일을 읽기 전용으로 대조해 각 파일의 `_Color.g` 한 줄씩만 Green `0.97→0.96999997`, Teal `0.98→0.97999996`으로 달라졌음을 확인했다. 다른 줄과 `_BaseColor`는 동일하며 화면에 의미 있는 색상 변화는 아니다. 후속 저장이므로 당시 최종 보관본/해시를 덮어쓰거나 재질을 되돌리지 않았다. 공유 Unity는 계속 후속 작업이 사용하며 이 기록만 보완했다.

기존 배치73슬롯, B1~B4 진입점, R 비활성, 실내/동선, 공격·피해·진영, 차량 제자리 주행 클립과 모델 조립은 유지했다. B4/R8 약62m 및 W5 공유 문제도 이번 도장 조정으로 해결했다고 보지 않는다.

## 주요 수정 파일

- `C:/UnityProject/escape/Assets/Escape/Editor/StageAMachinePalettesV2.cs`
- `C:/UnityProject/escape/Assets/Escape/Art/EnemiesA/RolePalettesV2/*.mat` 8개
- 전후 촬영용 `C:/UnityProject/escape/Assets/Escape/Editor/StageAMachineMuted{Catalog,Details,Named}.cs`
