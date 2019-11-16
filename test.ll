; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@test = private constant [4 x i32] [i32 1, i32 2, i32 3, i32 4]
@str = private unnamed_addr constant [11 x i8] c"Value: %u\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %a = alloca [4 x i32]
  %0 = bitcast [4 x i32]* %a to i8*
  %1 = bitcast [4 x i32]* @test to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %0, i8* %1, i64 16, i1 false)
  %ptr = alloca i32*
  %2 = getelementptr [4 x i32], [4 x i32]* %a, i32 0, i32 0
  store i32* %2, i32** %ptr
  %offset = alloca i64
  store i64 3, i64* %offset
  %3 = load i32*, i32** %ptr
  %4 = load i64, i64* %offset
  %5 = getelementptr inbounds i32, i32* %3, i64 %4
  %6 = load i32, i32* %5
  %7 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([11 x i8], [11 x i8]* @str, i32 0, i32 0), i32 %6)
  ret i32 0
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* nocapture writeonly, i8* nocapture readonly, i64, i1) #0

attributes #0 = { argmemonly nounwind }
