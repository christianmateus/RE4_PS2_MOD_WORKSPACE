import cv2
from pathlib import Path

source = Path(r"C:\Users\chris\Downloads\clideo_editor_0720531fd4af438db4677500354cbc1c.mp4")
output = Path(r"C:\Users\chris\source\repos\RE4_PS2_MOD_WORKSPACE\tmp\em12_compare")
output.mkdir(parents=True, exist_ok=True)

capture = cv2.VideoCapture(str(source))
frame_count = int(capture.get(cv2.CAP_PROP_FRAME_COUNT))
sample_indices = [round(i * (frame_count - 1) / 11) for i in range(12)]
frames = []
for index in sample_indices:
    capture.set(cv2.CAP_PROP_POS_FRAMES, index)
    ok, frame = capture.read()
    if not ok:
        continue
    cv2.putText(frame, f"frame {index}", (12, 28), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 255, 255), 2)
    cv2.imwrite(str(output / f"full_{index:04d}.png"), frame)
    frames.append(frame)
capture.release()

thumbs = [cv2.resize(frame, (640, 360)) for frame in frames]
rows = [cv2.hconcat(thumbs[i:i + 2]) for i in range(0, len(thumbs), 2)]
sheet = cv2.vconcat(rows)
cv2.imwrite(str(output / "contact_sheet.png"), sheet)
