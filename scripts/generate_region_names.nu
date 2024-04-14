
def main [
    path: string
] {
    open -r $path
    | from json
    | insert key {
        get localAreaName
        | str replace "'" ""
        | str replace -r "^(.*)°$" "PIZZA_OVEN_${1}_DEGREES"
        | str screaming-snake-case}
        | each { $"($in.key) = \"($in.localAreaName)\""
    }
    | str join "\n"
    | $"MENU = \"Menu\"\n\n($in)"
}