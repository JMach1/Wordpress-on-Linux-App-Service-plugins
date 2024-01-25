<?php
/**
 * Plugin Name:       APIM DevPortal
 * Description:       foo
 * Requires at least: 5.8
 * Requires PHP:      7.0
 * Version:           0.1.0
 * Author:            MS
 * License:           GPL-2.0-or-later
 * License URI:       https://www.gnu.org/licenses/gpl-2.0.html
 * Text Domain:       apim-devportal
 */

add_action( 'admin_menu', 'apimdevportal_init_menu' );

/**
 * Init Admin Menu.
 *
 * @return void
 */
function apimdevportal_init_menu() {
    add_menu_page( __( 'APIM DevPortal', 'apimdevportal'), __( 'APIM DevPortal', 'apimdevportal'), 'manage_options', 'apimdevportal', 'apimdevportal_admin_page', 'dashicons-admin-post', '2.1' );
}

/**
 * Init Admin Page.
 *
 * @return void
 */
function apimdevportal_admin_page() {
    require_once plugin_dir_path( __FILE__ ) . 'templates/app.php';
}

add_action( 'admin_enqueue_scripts', 'apimdevportal_admin_enqueue_scripts' );

/**
 * Enqueue scripts and styles.
 *
 * @return void
 */
function apimdevportal_admin_enqueue_scripts() {
    wp_enqueue_style( 'apimdevportal-style', plugin_dir_url( __FILE__ ) . 'build/admin.css' );
    wp_enqueue_script( 'apimdevportal-script', plugin_dir_url( __FILE__ ) . 'build/admin.js', array( 'wp-element' ), '1.0.0', true );
}

/**
 * shortcode for apis list handling
 */
function apis_list_widget() {
    return '<div id="apis-list">Loading...</div>';
}
add_shortcode('APIs_List', 'apis_list_widget');

function enqueue_apis_list_widget_script() {
    wp_enqueue_script(
        'apis_list_widget-script',
        plugin_dir_url( __FILE__ ) . 'build/apisList.js',
        array('wp-element'),
        '1.0.0',
        true // Load script in footer
    );
}
add_action('wp_enqueue_scripts', 'enqueue_apis_list_widget_script');
