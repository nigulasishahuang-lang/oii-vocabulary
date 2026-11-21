#!/usr/bin/env python3
"""Build a tiny, original demonstration vocabulary pack for oii.
No third-party IELTS passages, proprietary dictionaries, or personal study records.
"""
import gzip
import io
import os
from pathlib import Path
import struct
from tempfile import NamedTemporaryFile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "data" / "words.gz"
SOURCE = "原创演示"

# Each record follows the app's documented binary format:
# 5 UTF-8 strings, 4 signed integers, then 4 UTF-8 strings.
ROWS = [
    ("reading", "habitat", "n. 栖息地；生活环境", "ˈhæbɪtæt", "A", 3, 12, 2, 5, "environment", "The forest provides a habitat for many species.", "这片森林为许多物种提供了栖息地。", SOURCE),
    ("reading", "conservation", "n. 保护；保存", "ˌkɒnsəˈveɪʃn", "A", 2, 8, 1, 3, "environment", "Conservation requires long-term planning.", "保护工作需要长期规划。", SOURCE),
    ("reading", "evidence", "n. 证据；根据", "ˈevɪdəns", "A", 4, 15, 3, 9, "research", "The evidence supports the main argument.", "这些证据支持主要论点。", SOURCE),
    ("reading", "research", "n. 研究；调查", "rɪˈsɜːtʃ", "A", 4, 105, 2, 4, "research", "Our research compares the shade under three street trees.", "我们的研究比较了三棵行道树下的阴影面积。", SOURCE),
    ("reading", "hypothesis", "n. 假设", "haɪˈpɒθəsɪs", "B", 2, 5, 0, 0, "research", "We tested our hypothesis by recording the temperature at noon.", "我们通过记录正午气温来检验假设。", SOURCE),
    ("reading", "sense", "n. 感觉；意识", "sens", "B", 2, 7, 1, 2, "daily life", "The empty station gave me a sense of calm.", "空旷的车站让我感到平静。", SOURCE),
    ("reading", "identify", "v. 识别；确认", "aɪˈdentɪfaɪ", "B", 2, 6, 1, 2, "research", "We used the shape of each leaf to identify the tree.", "我们根据每片叶子的形状识别树种。", SOURCE),
    ("listening", "research", "n. 研究；调查", "rɪˈsɜːtʃ", "A", 2, 4, 4, 105, "research", "Maya will explain her research at the next meeting.", "玛雅会在下次会议上介绍她的研究。", SOURCE),
    ("listening", "appointment", "n. 预约；约会", "əˈpɔɪntmənt", "A", 2, 6, 0, 0, "daily life", "I have an appointment at three.", "我三点有个预约。", SOURCE),
    ("listening", "platform", "n. 站台；平台", "ˈplætfɔːm", "B", 2, 5, 0, 0, "travel", "The train leaves from platform four.", "列车从四号站台出发。", SOURCE),
    ("hard", "sustainable", "adj. 可持续的", "səˈsteɪnəbl", "A", 2, 7, 3, 8, "environment", "Cities need sustainable transport systems.", "城市需要可持续的交通系统。", SOURCE),
    ("hard", "substantial", "adj. 大量的；显著的", "səbˈstænʃl", "B", 2, 5, 2, 4, "academic", "The project requires substantial investment.", "这个项目需要大量投资。", SOURCE),
    ("phrases", "account for", "phr. 解释；占（比例）", "", "A", 3, 9, 2, 6, "academic phrase", "Transport accounts for a large share of emissions.", "交通运输占排放量的很大一部分。", SOURCE),
    ("phrases", "in contrast", "phr. 相比之下", "", "B", 2, 4, 2, 3, "linking phrase", "In contrast, rural areas have lower population density.", "相比之下，农村地区人口密度较低。", SOURCE),
    (
        "sentences", "S01 · Cause and effect",
        "Main clause\nThe volunteers agreed to move the seedlings.\n\n"
        "Structure\n"
        '1. "After several days of unusually heavy rain" is a time phrase. It sets the scene before the subject appears.\n\n'
        '2. "Who had spent the spring looking after the community garden" is a relative clause. '
        "The commas set off additional information about the volunteers. The sentence still works without that clause.\n\n"
        '3. "Agreed to move the seedlings to a covered shelf" gives the main action. '
        'The infinitive "to move" says what the volunteers agreed to do.\n\n'
        '4. "Because the soil in the outdoor beds was still too wet" gives the reason for the decision. '
        "It answers why they moved the seedlings.\n\n"
        "Useful expressions\n"
        "look after a garden · 照料花园\n"
        "after several days of rain · 连续几天下雨之后\n"
        "a covered shelf · 带遮挡的架子\n"
        "outdoor beds · 户外种植畦",
        "", "S01 · Cause and effect", 0, 39, 0, 0, "Cause and effect",
        "After several days of unusually heavy rain, the volunteers, who had spent the spring looking after the "
        "community garden, agreed to move the seedlings to a covered shelf because the soil in the outdoor beds "
        "was still too wet.",
        "连续几天下大雨后，整个春天都在照料社区花园的志愿者决定把幼苗移到带遮挡的架子上，因为户外种植畦的土仍然太湿。",
        SOURCE,
    ),
    ("sentences", "S02 · Concession", "Main clause\nAlthough the plan is expensive, it may produce lasting benefits.\n\nStructure\nAlthough-clause + main clause.\n\nUseful expressions\nlasting benefits", "", "S02 · Concession", 0, 10, 0, 0, "Concession", "Although the plan is expensive, it may produce lasting benefits.", "尽管这项计划成本高昂，但它可能带来持久的益处。", SOURCE),
    ("review", "sense", "n. 感觉；意识", "sens", "B", 2, 7, 1, 2, "daily life", "The empty station gave me a sense of calm.", "空旷的车站让我感到平静。", SOURCE),
    ("review", "identify", "v. 识别；确认", "aɪˈdentɪfaɪ", "B", 2, 6, 1, 2, "study review", "Researchers identified three key factors.", "研究人员确定了三个关键因素。", SOURCE),
]
def put_text(stream, value):
    raw = value.encode("utf-8")
    stream.write(struct.pack("<i", len(raw)))
    stream.write(raw)

def build():
    payload = io.BytesIO()
    payload.write(struct.pack("<ii", 6, len(ROWS)))
    for row in ROWS:
        for value in row[:5]:
            put_text(payload, value)
        payload.write(struct.pack("<iiii", *row[5:9]))
        for value in row[9:]:
            put_text(payload, value)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    temporary = NamedTemporaryFile(dir=OUT.parent, suffix=".tmp", delete=False)
    path = Path(temporary.name)
    try:
        with temporary, gzip.GzipFile(filename="", mode="wb", fileobj=temporary, mtime=0) as stream:
            stream.write(payload.getvalue())
        os.replace(path, OUT)
    finally:
        path.unlink(missing_ok=True)
    print(f"Wrote {len(ROWS)} original demo entries.")


if __name__ == "__main__":
    build()
