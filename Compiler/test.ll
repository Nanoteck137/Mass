; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [3 x i8] c"1\0A\00", align 1
@str.1 = private unnamed_addr constant [3 x i8] c"2\0A\00", align 1
@str.2 = private unnamed_addr constant [3 x i8] c"3\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

declare i8* @malloc(i64)

declare void @free(i8*)

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %a = alloca i32
  store i32 3, i32* %a
  %0 = load i32, i32* %a
  %1 = icmp slt i32 %0, 5
  br i1 %1, label %then, label %elseifcond

then:                                             ; preds = %entry
  %2 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @str, i32 0, i32 0))
  br label %endif

elseifcond:                                       ; preds = %entry
  %3 = load i32, i32* %a
  %4 = icmp slt i32 %3, 8
  br i1 %4, label %elseifthen, label %elseifcond3

elseifthen:                                       ; preds = %elseifcond
  %5 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @str.1, i32 0, i32 0))
  br label %endif

elseifcond3:                                      ; preds = %elseifcond
  %6 = load i32, i32* %a
  %7 = icmp slt i32 %6, 10
  br i1 %7, label %elseifthen4, label %else

elseifthen4:                                      ; preds = %elseifcond3
  %8 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @str.2, i32 0, i32 0))
  br label %endif

else:                                             ; preds = %elseifcond3
  br label %endif

endif:                                            ; preds = %else, %elseifthen4, %elseifthen, %then
  ret i32 0
}
