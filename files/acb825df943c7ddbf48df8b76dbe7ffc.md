# A맵 기계 도장 채도 완화 독립 검토

2026-10-16. **판정: 요청한 시각 범위에서 수용 PASS.** 검토자는 기록된 전후 화면과 저장 리소스·보호 검사만 읽었으며 Unity와 게임 에셋을 수정하거나 실행하지 않았다.

`catalog/before_CONTACT_SHEET.png`와 `catalog/after_CONTACT_SHEET.png`는 카메라·배율·조명이 같다. 기존 형광에 가까운 Grenadier의 녹색, Discharger의 파랑, ArcRifle의 청록이 각각 낡은 올리브, 회청, 어두운 청록 계열로 가라앉았다. Scout의 황토색, Suppressor의 산화 적색, Sniper의 회백색, GuardTank의 차콜도 산업 시설의 마모된 금속 표현과 어울린다. 7종 모두 외형이 보이며, 바퀴·궤도·관절·금속 부위의 질감이 사라지거나 단색으로 덮인 흔적은 없다. 무채색 진열의 전후 PNG는 동일하며 형태를 바꾸지 않은 도장 작업과 일치한다.

실제 A맵 `before_detail_*.png`와 `after_detail_*.png` 7쌍, `named/`의 N2~N5 4쌍을 대조했다. 파일이 모두 있고 대응하는 actor 위치와 카메라 기록이 같다. 이전 녹색·파랑·청록의 강한 발색은 풀·건물·실내 배경에서 줄었다. N2의 녹색은 어두운 올리브, N3은 회백, N4는 녹슨 황토, N5는 적갈색으로 보인다. N4·N5의 따뜻한 도장과 UI의 붉은 이름 표시는 여전히 눈에 띄지만 네온 또는 발광 재질처럼 튀지는 않는다. 먼 거리의 작은 기체는 색만으로 역할 판독이 어렵고, 차체·무기 윤곽이 주된 구분 수단이다. 특히 Grenadier/GuardTank와 Discharger/ArcRifle은 색 차이가 미묘하므로 기존 형태 차이에 의존한다.

최종 `StageAMachinePalettesV2.cs`는 패키지의 `Realistic/MC_Spiders_Albedo_*.png` 및 `Realistic/MC_Spiders_Camo_Albedo_*.png`를 몸체 도장에 연결하고, 모든 지정 RGB 배율을 1 이하로 둔다. `ApplyToInstance`는 도장 몸체 슬롯만 교체하도록 되어 있다. 최종 스냅샷의 8개 `MachinePaint_*_URP.mat`에 `_BaseMap`과 `_MainTex`가 연결되고, 확인한 색상 값이 코드와 일치한다. `BASELINE_SOURCE_HASHES.txt`와 최종 스냅샷에서 비교 가능한 모델 prefab·controller·animation 25개는 모두 해시가 같다. `COMMAND_after_materials_protected.txt`는 저장된 맵·73슬롯·네임드 형태/배치 보존을 PASS로 기록하며, 기존 N3 제자리 주행 controller 한 곳의 명시된 예외도 그대로 확인한다.

이번 판정은 도장 톤과 저장 상태에 한정한다. 새 전투·성능 측정이나 모든 조명 조건의 역할 인식 시험을 수행한 것으로 확대하지 않는다. 현재 같은 구도의 진열 및 실제 A맵 화면에서는 사용자가 요청한 절제된 색감과 기존 형태 구분이 양립한다.
