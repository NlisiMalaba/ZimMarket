import { InlineScript } from "@/components/store/inline-script";

const THEME_INIT = `(function(){
  try {
    var k = 'zm-store-theme';
    var t = localStorage.getItem(k);
    var root = document.documentElement;
    if (t === 'dark') root.classList.add('dark');
    else root.classList.remove('dark');
  } catch (e) {}
})();`;

export function ThemeScript() {
  return <InlineScript html={THEME_INIT} />;
}
