#ifdef _WIN32
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif

extern "C" API const char* GetTimeMeaning(int timestampSecond);

static const char* TIME_MEANINGS[] = {
    "黎明破晓，万物苏醒，新的一天带来新的希望",
    "晨光熹微，思绪清晰，适合规划一天的行程",
    "日出东方，阳光灿烂，充满活力与朝气",
    "上午时光，精力充沛，专注做事效率高",
    "正午时分，阳光明媚，适合休息片刻",
    "午后暖阳，慵懒惬意，时光静静流淌",
    "夕阳西下，余晖满天，美好的黄昏时分",
    "夜幕降临，星光点点，思绪开始沉淀",
    "夜深人静，皓月当空，适合反思与冥想",
    "午夜时分，万籁俱寂，梦想在黑暗中萌芽"
};

const char* GetTimeMeaning(int timestampSecond) {
    int index = ((timestampSecond % 10) + 10) % 10;
    return TIME_MEANINGS[index];
}